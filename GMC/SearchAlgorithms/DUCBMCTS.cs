using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// A variant of MCTS that uses the Discounted UCB policy as its tree policy. Differs from, DUCBMCT2 in that it
    /// uses the DUCBMCTSNode implementation to represent is nodes, which means that it doesn't use its discounted
    /// estimates when picking a move at the end of its run.
    /// </summary>
    internal class DUCBMCTS : BasicMCTS
    {
        /// <summary>
        /// Determines whether the exploration part of UCB should be computed as usual,
        /// or the way that it is computed in D-UCB in the original paper.
        /// </summary>
        private bool longExplore;

        public static new ISearch TryGetInstance(string specification)
        {
            if (specification.StartsWith("ducbmcts_"))
            {
                string[] parts = specification.Split('_');
                double gamma = double.Parse(parts[1]);
                bool longExplore = bool.Parse(parts[2]);
                bool useDiscountedEstimates = bool.Parse(parts[3]);

                if (parts.Length == 5)
                {
                    double c = double.Parse(specification.Split('_')[parts.Length - 1]);
                    return new DUCBMCTS(gamma, longExplore, useDiscountedEstimates, c);
                }
                else
                {
                    return new DUCBMCTS(gamma, longExplore, useDiscountedEstimates);
                }
            }

            return null;
        }

        public DUCBMCTS(double gamma, bool longExplore, bool useDiscountedEstimates)
        {
            DMCTSNode.gamma = gamma;
            this.longExplore = longExplore;
            DMCTSNode.useDiscountedEstimates = useDiscountedEstimates;
        }

        public DUCBMCTS(double gamma, bool longExplore, bool useDiscountedEstimates, double c) : base(c)
        {
            DMCTSNode.gamma = gamma;
            this.longExplore = longExplore;
            DMCTSNode.useDiscountedEstimates = useDiscountedEstimates;
        }

        public override void Initialize(TreeNode initialState)
        {
            root = new DMCTSNode(initialState, initialState.CurrentPlayer, null);
            DMCTSNode.initialTeam = initialState.CurrentPlayer;
        }

        protected override double UCB(MCTSNode n)
        {
            if (longExplore)
            {
                return ((DMCTSNode)n).GetDiscountedScore() + c * Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
            }
            else
            {
                // Due to multiplying by gamma during every backpropagation, the discounted visits variable
                // in the parent doesn't equal the sum of discounted visits variables in the children, so the
                // sum has to be computed separately here.
                double discountedParentVisits = 0;
                foreach (DMCTSNode child in n.parent.Children)
                {
                    discountedParentVisits += child.DiscountedVisits;
                }

                return ((DMCTSNode)n).GetDiscountedScore() + c * Math.Sqrt(Math.Log(discountedParentVisits) / ((DMCTSNode)n).DiscountedVisits);
            }
        }
    }
}
