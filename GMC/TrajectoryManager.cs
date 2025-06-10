using GMC.SearchAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// A class capable of printing given types of rewards encountered during simulations.
    /// These can than be used to run different types of computations to see which achieves
    /// best results, provided that the tested algorithm only makes random simulations (so
    /// it's useless for MCTS, but can be used when testing Random Simulation Search, which
    /// is a part of MCTS).
    /// </summary>
    internal class TrajectoryManager
    {
        /// <summary>
        /// Runs a given number of random simulations on a given tree while writing down the encountered rewards.
        /// </summary>
        /// <param name="sourceFile"> The tree file from which the simulations are supposed to be generated.  </param>
        /// <param name="numOfSimulations"> The number of simulations to run. </param>
        /// <param name="rtype"> The type of reward to gather. </param>
        /// <param name="outputPath"> Path to the file into which the data is to be written. </param>
        public static void PrintTrajectories(
            string sourceFile,
            int numOfSimulations,
            RewardType rewardToApproximate,
            RewardType rtype,
            string outputPath
            )
        {
            TreeNode root = TreeNode.GetRoot(sourceFile);
            bool player = root.CurrentPlayer;

            using (StreamWriter output = new StreamWriter(outputPath))
            {
                // Write the real value on the first line.
                output.WriteLine(root.GetRewardFor(player, rewardToApproximate));

                // Run the given algorithm for the given number of iterations and gather the data.
                for (int iterationCounter = 0; iterationCounter < numOfSimulations; ++iterationCounter)
                {
                    TreeNode currentState = root;
                    while (currentState != null)
                    {
                        output.Write(currentState.GetRewardFor(player, rtype) + ",");
                        
                        if (currentState.isTerminal)
                        {
                            currentState = null;
                        }
                        else
                        {
                            currentState = currentState.GetRandomChild();
                        }
                    }
                    output.WriteLine();
                }
            }
        }

        /// <summary>
        /// Evaluates the given trajectories from the point of view of a given algorithm.
        /// </summary>
        /// <param name="algorithm"> The algorithm to use. </param>
        /// <param name="inputPath"> Path to the file that contains the trajectories. </param>
        /// <param name="outputPath"> Output file path. </param>
        public static void ProcessTrajectories(
            ISearch algorithm,
            string inputPath,
            string outputPath
            )
        {
            using (StreamReader sr = new StreamReader(inputPath))
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                // Read the real reward from the first line.
                double trueReward = double.Parse(sr.ReadLine());
                double currentAccumReward = 0;
                int currentNumOfSims = 0;

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] rewStrings = line.Split(',');
                    List<double> rewards = new List<double>();

                    int maxLength = algorithm.GetMaximumSimulationLength();
                    
                    foreach(string r in rewStrings)
                    {
                        if (r != "")
                        {
                            rewards.Add(double.Parse(r));
                        }

                        if (rewards.Count == maxLength)
                        {
                            break;
                        }
                    }

                    currentAccumReward += algorithm.EvaluateTrajectory(rewards);
                    ++currentNumOfSims;

                    sw.Write(Math.Abs(trueReward - currentAccumReward / currentNumOfSims) + ",");
                }
            }
        }
    }
}
