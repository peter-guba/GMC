using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// An MCTS node implementation that stores the individual observed rewards, not just their sum, in order to
    /// be able to use the Sliding Window UCB policy. The acronym stands for Whole Reward Trail MCTS Node. It
    /// differs from WRTMCTSNode in that it overrides the GetScore method, so its windowed estimates are also
    /// used when picking moves at the end of MCTS.
    /// </summary>
    internal class WRTMCTSNode2 : MCTSNode
    {
        /// <summary>
        /// A list of all the rewards observed thus far.
        /// </summary>
        private List<double> rewards = new List<double>();

        /// <summary>
        /// The number of rewards to be taken into account.
        /// </summary>
        public static int windowSize;

        public WRTMCTSNode2(TreeNode state, bool team, MCTSNode parent) : base(state, team, parent) { }

        public override void UpdateValue(double val)
        {
            Value += val;
            rewards.Add(val);
            ++Visits;
        }

        public override MCTSNode Expand()
        {
            WRTMCTSNode2 newNode = new WRTMCTSNode2(State.GetNextChild(), !team, this);
            Children.Add(newNode);

            if (Children.Count == State.Children.Count)
            {
                FullyExpanded = true;
            }

            return newNode;
        }

        /// <summary>
        /// Returns the classic mean value (sum of rewards divided by their number).
        /// </summary>
        public double GetWindowedScore()
        {
            return GetRewards().Sum() / Math.Min(windowSize, rewards.Count);
        }

        public override double GetScore()
        {
            return GetWindowedScore();
        }

        /// <summary>
        /// Returns the last n rewards where n is either the window size, or the number of times the node
        /// has been visited in the last window (depending on whether longExplore is turned on).
        /// </summary>
        public List<double> GetRewards()
        {
            if (windowSize == 0 || windowSize > rewards.Count)
            {
                return rewards;
            }
            else
            {
                return rewards.GetRange(rewards.Count - windowSize, windowSize);
            }
        }
    }
}
