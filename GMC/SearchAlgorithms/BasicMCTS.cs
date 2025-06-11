using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// An implementation of the basic version of MCTS. It uses UCB as its tree policy, random moves as its
    /// default policy and picks a move based on its estimate of moves' mean rewars at the end of its run.
    /// </summary>
    internal class BasicMCTS : ISearch
    {
        protected MCTSNode root = null;

        protected readonly double c = Math.Sqrt(2);

        /// <summary>
        /// Limits the depth of every simulation if set to a value larger than 0.
        /// It is only possible to do so through classes that inherit from this class.
        /// </summary>
        protected int simDepth = -1;

        /// <summary>
        /// Tries to create an instance based on the given string specification.
        /// If the specification doesn't fit, it returns null.
        /// </summary>
        public static ISearch TryGetInstance(string specification)
        {
            if (specification == "mcts")
            {
                return new BasicMCTS();
            }

            if (specification.StartsWith("mcts_"))
            {
                double c = double.Parse(specification.Split('_')[1]);

                return new BasicMCTS(c);
            }

            return null;
        }

        public BasicMCTS()
        { }

        public BasicMCTS(double c)
        {
            this.c = c;
        }

        public virtual void Initialize(TreeNode initialState)
        {
            root = new MCTSNode(initialState, initialState.CurrentPlayer, null);
            MCTSNode.initialTeam = initialState.CurrentPlayer;
        }

        public bool WouldNodeBeChosen(TreeNode node)
        {
            if (!root.FullyExpanded)
            {
                return false;
            }

            MCTSNode bestNode = null;
            double bestScore = 0;
            foreach (MCTSNode child in root.Children)
            {
                double score = child.GetScore();
                if (bestNode == null || score > bestScore)
                {
                    bestNode = child;
                    bestScore = score;
                }
            }

            return bestNode.State == node;
        }

        public bool WouldOneOfBestNodesBeChosen(double bestMinimaxScore)
        {
            if (!root.FullyExpanded)
            {
                return false;
            }

            MCTSNode bestNode = null;
            double bestScore = 0;
            foreach (MCTSNode child in root.Children)
            {
                double score = child.GetScore();
                if (bestNode == null || score > bestScore)
                {
                    bestNode = child;
                    bestScore = score;
                }
            }

            return bestNode.State.GetRewardFor(root.team, RewardType.Minimax) == bestMinimaxScore;
        }

        public virtual SearchNode GetCorrespondingRootChild(TreeNode node)
        {
            foreach (MCTSNode child in root.Children)
            {
                if (child.State == node)
                {
                    return child;
                }
            }

            return null;
        }

        public virtual SearchNode RunIteration()
        {
            MCTSNode chosenNode = Select(root);

            if (!chosenNode.State.isTerminal)
            {
                chosenNode = chosenNode.Expand();
            }

            TreeNode terminalState = chosenNode.Simulate(simDepth);
            double result = EvaluateGame(terminalState, chosenNode);
            MCTSNode nodeOfIterest = chosenNode.Backpropagate(result);

            return nodeOfIterest;
        }

        protected virtual MCTSNode Select(MCTSNode start)
        {
            // If the current node has already been fully expanded, pick one of its
            // children using the UCB policy.
            if (start.FullyExpanded)
            {
                // If the node has no children it means its terminal, so it is returned.
                if (start.Children.Count == 0)
                {
                    return start;
                }

                // Pick the best child according to UCB.
                MCTSNode bestChild = null;
                double bestScore = 0.0f;
                foreach (MCTSNode child in start.Children)
                {
                    double score = UCB(child);
                    if (bestChild == null || bestScore < score)
                    {
                        bestChild = child;
                        bestScore = score;
                    }
                }

                return Select(bestChild);
            }
            // Otherwise return this node.
            else
            {
                return start;
            }
        }

        protected virtual double EvaluateGame(TreeNode terminalState, MCTSNode node)
        {
            return terminalState.GetRewardFor(!node.team, RewardType.Minimax);
        }

        protected virtual double UCB(MCTSNode n)
        {
            return n.GetScore() + c * Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
        }

        public virtual double EvaluateTrajectory(List<double> rewards)
        {
            return rewards.Last();
        }

        public virtual int GetMaximumSimulationLength()
        {
            return int.MaxValue;
        }
    }
}
