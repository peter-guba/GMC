using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// A variant of MCTS that uses the Discounted UCB policy as its tree policy. Differs from, DUCBMCTS in that it
    /// uses the DUCBMCTSNode2 implementation to represent is nodes, which means that it also uses its discounted
    /// estimates when picking a move at the end of its run.
    /// </summary>
    internal class DUCBMCTS2 : BasicMCTS
    {
        /// <summary>
        /// Determines whether the exploration part of UCB should be computed as usual,
        /// or the way that it is computed in D-UCB in the original paper.
        /// </summary>
        private bool longExplore;

        public static new ISearch TryGetInstance(string specification)
        {
            if (specification.StartsWith("ducbmcts2_"))
            {
                string[] parts = specification.Split('_');
                double gamma = double.Parse(parts[1]);
                bool longExplore = bool.Parse(parts[2]);

                if (parts.Length == 4)
                {
                    double c = double.Parse(specification.Split('_')[3]);
                    return new DUCBMCTS2(gamma, longExplore, c);
                }
                else
                {
                    return new DUCBMCTS2(gamma, longExplore);
                }
            }

            return null;
        }

        public DUCBMCTS2(double gamma, bool longExplore)
        {
            DMCTSNode2.gamma = gamma;
            this.longExplore = longExplore;
        }

        public DUCBMCTS2(double gamma, bool longExplore, double c) : base(c)
        {
            DMCTSNode2.gamma = gamma;
            this.longExplore = longExplore;
        }

        public override void Initialize(TreeNode initialState)
        {
            root = new DMCTSNode2(initialState, initialState.CurrentPlayer, null);
            DMCTSNode2.initialTeam = initialState.CurrentPlayer;
        }

        protected override double UCB(MCTSNode n)
        {
            if (longExplore)
            {
                return ((DMCTSNode2)n).GetDiscountedScore() + c * Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
            }
            else
            {
                // Due to multiplying by gamma during every backpropagation, the discounted visits variable
                // in the parent doesn't equal the sum of discounted visits variables in the children, so the
                // sum has to be computed separately here.
                double discountedParentVisits = 0;
                foreach (DMCTSNode2 child in n.parent.Children)
                {
                    discountedParentVisits += child.DiscountedVisits;
                }

                return ((DMCTSNode2)n).GetDiscountedScore() + c * Math.Sqrt(Math.Log(discountedParentVisits) / ((DMCTSNode2)n).DiscountedVisits);
            }
        }
    }
}
