using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// A variant of MCTS that uses the Sliding Window UCB policy as its tree policy. It differs from SWUCBMCTS2
    /// in that it doesn't use its windowed estimates when picking moves at the end of its run.
    /// </summary>
    internal class SWUCBMCTS : BasicMCTS
    {
        /// <summary>
        /// Determines whether the exploration part of UCB should be computed as usual,
        /// or the way that it is computed in SW-UCB in the original paper.
        /// </summary>
        private bool longExplore;

        public static new ISearch TryGetInstance(string specification)
        {
            if (specification.StartsWith("swucbmcts_"))
            {
                string[] parts = specification.Split('_');
                int window = int.Parse(parts[1]);
                bool longExplore = bool.Parse(parts[2]);

                if (parts.Length == 4)
                {
                    double c = double.Parse(parts[parts.Length - 1]);
                    return new SWUCBMCTS(window, longExplore, c);
                }
                else
                {
                    return new SWUCBMCTS(window, longExplore);
                }
            }

            return null;
        }

        public SWUCBMCTS(int window, bool longExplore)
        {
            WRTMCTSNode.windowSize = window;
            this.longExplore = longExplore;
        }

        public SWUCBMCTS(int window, bool longExplore, double c) : base(c)
        {
            WRTMCTSNode.windowSize = window;
            this.longExplore = longExplore;
        }

        public override void Initialize(TreeNode initialState)
        {
            root = new WRTMCTSNode(initialState, initialState.CurrentPlayer, null);
            WRTMCTSNode.initialTeam = initialState.CurrentPlayer;
        }

        protected override MCTSNode Select(MCTSNode start)
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

                // Pick the best child according to UCB and call SelectAndExpand
                // on it.
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

        protected override double UCB(MCTSNode n)
        {
            double rewardsSum = ((WRTMCTSNode)n).GetRewards().Sum();

            if (longExplore)
            {
                return rewardsSum / Math.Min(WRTMCTSNode.windowSize, n.Visits) + c * Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
            }
            else
            {
                double resizedWindow = 0;

                foreach (MCTSNode child in n.parent.Children)
                {
                    resizedWindow += Math.Min(child.Visits, WRTMCTSNode2.windowSize);
                }

                return rewardsSum / Math.Min(WRTMCTSNode2.windowSize, n.Visits) + c * Math.Sqrt(Math.Log(resizedWindow) / Math.Min(WRTMCTSNode2.windowSize, n.Visits));
            }
        }
    }
} 
