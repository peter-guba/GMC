using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GMC.SearchAlgorithms;

namespace GMC
{
    /// <summary>
    /// A class capable of running an algorithm on a tree and measuring its rate of convergence to some
    /// values (either minimax or win ratio).
    /// </summary>
    internal class ConvergenceMeasurer
    {
        /// <summary>
        /// Runs an algorithm on a tree while gathering data about its rate of convergence.
        /// </summary>
        /// <param name="algorithm"> The algorithm to run. </param>
        /// <param name="sourceFile"> The source file for the tree. </param>
        /// <param name="numOfIterations"> The number of iterations of the algorithm that should be run. </param>
        /// <param name="perNode"> Determines whether the number of iterations should be interpreted as the
        /// total number of iterations or the number per child of the root. </param>
        /// <param name="rtype"> The type of reward to pay attention to. </param>
        /// <param name="outputPath"> The path leading to the directory where the output files should be created. </param>
        /// <param name="addBestFlag"> If set to true, only data about the best choice is gahthered. Otherwise,
        /// data on all the children of the root node are gathered in separate files.</param>
        /// <param name="moreThanOneChildNecessary"> Determines wheteher the algorithm should descend the
        /// tree until it finds a node that has more than one child and use that as the root, instead of using
        /// the first node as the root (since, in some trees, the root only has one child, which would then
        /// render measuring whether the best node was picked useless, as the child would always be picked). </param>
        public static void MeasureConvergence(
            ISearch algorithm,
            string sourceFile,
            int numOfIterations,
            bool perNode,
            RewardType rtype,
            string outputPath,
            bool addBestFlag,
            bool moreThanOneChildNecessary
            )
        {
            TreeNode root = TreeNode.GetRoot(sourceFile);

            if (moreThanOneChildNecessary)
            {
                root.LoadChildren();

                while (root.Children.Count == 1)
                {
                    root = root.Children[0];
                    root.LoadChildren();
                }
            }

            algorithm.Initialize(root);
            TreeNode bestChoice = null;
            double bestChoiceMMVal = 0;

            if (addBestFlag)
            {
                bestChoice = GetBestChoice(root, rtype);
                bestChoiceMMVal = bestChoice.GetRewardFor(root.CurrentPlayer, RewardType.Minimax);
            }

            int iterationMultiplier = 1;
            if (perNode)
            {
                iterationMultiplier = root.GetNumberOfChildren();
            }

            using (StreamWriter output = new StreamWriter(outputPath))
            {
                // Run the given algorithm for the given number of iterations and gather the data.
                for (int iterationCounter = 0; iterationCounter < iterationMultiplier * numOfIterations; ++iterationCounter)
                {
                    SearchNode visited = algorithm.RunIteration();
                    SearchNode toProcess = addBestFlag ? algorithm.GetCorrespondingRootChild(bestChoice) : visited;

                    if (toProcess != null)
                    {
                        double currentReward = toProcess.GetScore();
                        double trueReward = toProcess.State.GetRewardFor(root.CurrentPlayer,rtype);
                        double diff = Math.Abs(trueReward - currentReward);

                        if (addBestFlag)
                        {
                            int isBest = algorithm.WouldNodeBeChosen(bestChoice) ? 1 : 0;
                            int isOneOfBest = algorithm.WouldOneOfBestNodesBeChosen(bestChoiceMMVal) ? 1 : 0;
                            output.Write(isBest + "|" + isOneOfBest + "|" + diff + ",");
                        }
                        else
                        {
                            output.Write(diff + ",");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the best child of the root node according to the given reward type.
        /// If minimax value is used, the nodes are first selected based on this value,
        /// but if there is more than one node with the best minimax value, one of them
        /// is selected based on the win ratio.
        /// </summary>
        private static TreeNode GetBestChoice(TreeNode root, RewardType rtype)
        {
            bool player = root.CurrentPlayer;

            if (rtype == RewardType.WinRatio)
            {
                TreeNode best = null;
                double bestWR = 0;

                root.LoadChildren();
                foreach (TreeNode child in root.Children)
                {
                    double childWR = child.GetRewardFor(player, RewardType.WinRatio);
                    if (best == null || bestWR < childWR)
                    {
                        best = child;
                        bestWR = childWR;
                    }
                }

                return best;
            }
            else if (rtype == RewardType.Minimax)
            {
                TreeNode best = null;
                double bestWR = 0;
                double bestMM = 0;

                root.LoadChildren();
                foreach (TreeNode child in root.Children)
                {
                    double childMM = child.GetRewardFor(player, RewardType.Minimax);
                    double childWR = child.GetRewardFor(player, RewardType.WinRatio);

                    if (best == null || bestMM < childMM ||
                        (bestMM == childMM && bestWR < childWR))
                    {
                        best = child;
                        bestWR = childWR;
                        bestMM = childMM;
                    }
                }

                return best;
            }
            else
            {
                throw new NotImplementedException("Picking the best move isn't implemented for the reward type " + rtype.ToString());
            }
        }
    }
}
