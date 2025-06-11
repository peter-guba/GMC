using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// An MCTS node implementation that multiplies the accumulated reward and number of visits by a discount factor
    /// every time. Differs from DMCTSNode2 in that it doesn't override the GetScore method, which means that it doesn't
    /// use its discounted values when picking a move at the end of its run.
    /// </summary>
    internal class DMCTSNode : MCTSNode
    {
        /// <summary>
        /// The multiplicative factor that gets applied to observed rewards.
        /// </summary>
        public static double gamma;

        /// <summary>
        /// The sum of accumulated observed rewards, but each reward gets multiplied by gamma
        /// every time a new reward is observed.
        /// </summary>
        public double DiscountedValue { get; private set; }

        /// <summary>
        /// The number of times this node hasb een visited, but it gets multiplied by gamma
        /// before each increment.
        /// </summary>
        public double DiscountedVisits { get; private set; }

        public DMCTSNode(TreeNode state, bool team, MCTSNode parent) : base(state, team, parent)
        {
            DiscountedValue = 0;
            DiscountedVisits = 0;
        }

        public override MCTSNode Expand()
        {
            MCTSNode newNode = new DMCTSNode(State.GetNextChild(), !team, this);
            Children.Add(newNode);

            if (Children.Count == State.Children.Count)
            {
                FullyExpanded = true;
            }

            return newNode;
        }

        public override void UpdateValue(double val)
        {
            Value += val;
            ++Visits;
            // Have to project the values to between 1 and -1 here, so that the discount factor pulls them
            // towards neutral.
            DiscountedValue = DiscountedValue * gamma + val;
            DiscountedVisits = DiscountedVisits * gamma + 1;
        }

        public double GetDiscountedScore()
        {
            // Re-project the discounted values to between 1 and 0 again
            return DiscountedValue / DiscountedVisits;
        }
    }
}
