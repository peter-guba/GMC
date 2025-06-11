using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// The base class for search nodes of algorithms. Allows running simulations from the state it
    /// corresponds to.
    /// </summary>
    internal class SearchNode
    {
        /// <summary>
        /// The game state to which the search node corresponds (represented by a node of the
        /// underlying game tree).
        /// </summary>
        public TreeNode State { get; private set; }

        /// <summary>
        /// The value accumulated in this node during the playouts that passed through it.
        /// </summary>
        public double Value { get; protected set; }

        /// <summary>
        /// The number of times this node has been visited.
        /// </summary>
        public double Visits { get; protected set; }

        /// <summary>
        /// The team whose turn it is in the state corresponding to this node.
        /// true <=> white
        /// </summary>
        public readonly bool team;

        /// <summary>
        /// A random number generator used when generating moves during playouts.
        /// </summary>
        protected static Random rand = new Random();

        /// <summary>
        /// The player whose move it is at the root node.
        /// </summary>
        public static bool initialTeam;

        public SearchNode(TreeNode state, bool team)
        {
            State = state;
            this.team = team;
        }

        /// <summary>
        /// Performs a playout from this node.
        /// </summary>
        /// <param name="maxDepth"> Determines the maximum simulation depth. If it is set to zero
        /// or less, it is ignored. </param>
        public virtual TreeNode Simulate(int maxDepth = -1)
        {
            int numOfSteps = 0;
            bool currentTeam = team;
            TreeNode currentState = State;

            // Keep making moves until a terminal state is reached.
            while (!currentState.isTerminal && (maxDepth <= 0 || numOfSteps < maxDepth))
            {
                currentState = currentState.GetRandomChild();
                currentTeam = !currentTeam;
                ++numOfSteps;
            }

            return currentState;
        }

        public virtual void UpdateValue(double val)
        {
            Value += val;
            ++Visits;
        }

        public virtual double GetScore()
        {
            return Value / Visits;
        }
    }
}
