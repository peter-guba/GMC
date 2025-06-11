using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// An interface that is to be implemented by any search algorithm.
    /// </summary>
    internal interface ISearch
    {
        void Initialize(TreeNode initialState);

        /// <summary>
        /// Determines whether the algorithm would pick the given node using the node scores it has
        /// computed thus far.
        /// </summary>
        bool WouldNodeBeChosen(TreeNode node);

        /// <summary>
        /// Determines whether one of the nodes with the best minimax score (which is supplied by a parameter) would
        /// be chosen using the node scores computes thus far.
        /// </summary>
        bool WouldOneOfBestNodesBeChosen(double bestMinimaxScore);

        /// <summary>
        /// Gets the child of the root that corresponds to the given tree node.
        /// </summary>
        SearchNode GetCorrespondingRootChild(TreeNode node);

        /// <summary>
        /// Runs a single iteration of the algorithm. Should only be called after initialization has been called.
        /// </summary>
        /// <returns> The node of interest that was visited in this iteration. </returns>
        SearchNode RunIteration();

        /// <summary>
        /// Computes the reward the algorithm would give a given game trajectory.
        /// </summary>
        double EvaluateTrajectory(List<double> rewards);

        /// <summary>
        /// Returns the maximum allowed simulation length for the algorithm, or int.MaxValue if no such
        /// cutoff point exists.
        /// </summary>
        int GetMaximumSimulationLength();
    }
}
