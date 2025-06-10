using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Defines methods for estimating the sizes of trees generated using given parameters.
    /// </summary>
    internal class SizeEstimator
    {
        private static Random rand = new Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// Computes the expected size of a generated file given the generation
        /// settings.
        /// </summary>
        /// <param name="startMaxBranchingFactor"> The maximum branching factor at
        /// the start of tree generation. </param>
        /// <param name="endMaxBranchingFactor"> The maximum branching factor at the
        /// end of tree generation. </param>
        /// <param name="tpc"> An object capable of computing the probability
        /// of a node being terminal at a given depth. </param>
        /// <param name="maxDepth"> The maximum depth of the tree. </param>
        /// <param name="nodeSizeEstimate"> An estimate of the size of a node. </param>
        public static double EstimateMeanFileSize(
            double startMaxBranchingFactor,
            double endMaxBranchingFactor,
            ITerminationProbabilityCalculator tpc,
            int maxDepth,
            int nodeSizeEstimate
            )
        {
            // The expected number of nodes in the whole file.
            double expectedNumOfNodes = 1;

            // The expected number of nodes in the current depth.
            double expNumOfNodesInCurrentDepth = 1;

            // The expected number of nonterminal nodes in the whole tree.
            double expNumOfNonterminals = 1;

            // The expected number of lines which will separate different nodes
            // and different levels.
            double expNumOfLevelAndNodeSeparators = 0;

            int separatorLength = 2;

            // Termination probability in the previous iteration.
            double prevTP = 0.0;

            for (int currentDepth = 1; currentDepth <= maxDepth; ++currentDepth)
            {
                // Compute the expected branching factor at the current depth.
                int currentMaxBranchingFactor = ((maxDepth - (currentDepth - 1)) * (int)startMaxBranchingFactor + (currentDepth - 1) * (int)endMaxBranchingFactor) / maxDepth;
                double expectedBranchingFactor = (currentMaxBranchingFactor + 1.0) / 2;

                expNumOfLevelAndNodeSeparators += expNumOfNodesInCurrentDepth;

                // Compute the expected number of nodes in the current depth and update the file
                // size estimate.
                expNumOfNodesInCurrentDepth *= (1 - prevTP) * expectedBranchingFactor;
                expectedNumOfNodes += expNumOfNodesInCurrentDepth;

                if (currentDepth != maxDepth)
                {
                    // Compute the expected number of nonterminals in the current level and add it to
                    // expNumOfNonterminals.
                    double tP = tpc.CalculateCurrentTerminationProbability();
                    expNumOfNonterminals += expNumOfNodesInCurrentDepth * (1 - tP);
                    prevTP = tP;
                }

                // 1 level separator.
                ++expNumOfLevelAndNodeSeparators;
            }

            // An estimate of the nnumber of characters needed to specify the positions of children.
            double posSpecificationLengthEstimate = Math.Floor(Math.Log10(expectedNumOfNodes * nodeSizeEstimate) + 1) + 1;

            return expectedNumOfNodes * nodeSizeEstimate +
                expNumOfNonterminals * posSpecificationLengthEstimate +
                expNumOfLevelAndNodeSeparators * separatorLength;
        }

        // Note that the estimate that the EstimateMeanFileSize method computes is a little off because the
        // posSpecificationLengthEstimate is nonlinear. However the difference is negligible and the estimate should still
        // be better when it comes to larger trees than what could be obtained by using random sampling.

        /// <summary>
        /// Estimates the variance of the size of a file given its settings by randomly sampling
        /// tree sizes and averaging their squared deviation from the mean.
        /// </summary>
        /// <param name="meanSize"> The mean file size. </param>
        /// <param name="numOfSamples"> The number of samples that is supposed to be used for the estimation. </param>
        /// <param name="startMaxBranchingFactor"> The maximum branching factor at
        /// the start of tree generation. </param>
        /// <param name="endMaxBranchingFactor"> The maximum branching factor at the
        /// end of tree generation. </param>
        /// <param name="tpc"> An object capable of computing the probability
        /// of a node being terminal at a given depth. </param>
        /// <param name="maxDepth"> The maximum depth of the tree. </param>
        /// <param name="nodeSizeEstimate"> An estimate of the size of a node. </param>
        public static double EstimateFileSizeStandardDeviation(
            double meanSize,
            int numOfSamples,
            double startMaxBranchingFactor,
            double endMaxBranchingFactor,
            ITerminationProbabilityCalculator tpc,
            int maxDepth,
            int nodeSizeEstimate
            )
        {
            double sum = 0.0;

            for (int iterationCounter = 0; iterationCounter < numOfSamples; ++iterationCounter)
            {
                tpc.Reset();

                // The number of nodes in the random tree.
                long numOfNodes = 1;

                // The number of nodes in the current depth.
                long numOfNodesInCurrentDepth = 1;

                // The number of nonterminal nodes in the whole tree.
                double numOfNonterminals = 0;

                // The expected number of lines which will separate different nodes
                // and different levels.
                double numOfLevelAndNodeSeparators = 0;

                int separatorLength = 2;

                // Termination probability in the level constructed in the previous iteration.
                double prevTP = 0.0;

                for (int currentDepth = 1; currentDepth <= maxDepth; ++currentDepth)
                {
                    int currentMaxBranchingFactor = ((maxDepth - currentDepth) * (int)startMaxBranchingFactor + currentDepth * (int)endMaxBranchingFactor) / maxDepth;

                    numOfLevelAndNodeSeparators += numOfNodesInCurrentDepth;

                    // For every node, randomly decide if its terminal using the prevTP variable as the bound
                    // and if it isn't, generate a random number of children (with currentMaxBranchingFactor being
                    // the maximum possible number of children).
                    long newNodes = 0;
                    for (long nodeIterator = 0; nodeIterator < numOfNodesInCurrentDepth; ++nodeIterator)
                    {
                        if (rand.NextDouble() > prevTP)
                        {
                            ++numOfNonterminals;
                            newNodes += rand.Next(currentMaxBranchingFactor) + 1;
                        }
                    }
                    numOfNodesInCurrentDepth = newNodes;
                    numOfNodes += numOfNodesInCurrentDepth;

                    // Update the termination probability.
                    if (currentDepth != maxDepth)
                    {
                        prevTP = tpc.CalculateCurrentTerminationProbability();
                    }

                    // 1 level separator.
                    ++numOfLevelAndNodeSeparators;
                }

                // An estimate of the nnumber of characters needed to specify the positions of children.
                double posSpecificationLengthEstimate = Math.Floor(Math.Log10(numOfNodes * nodeSizeEstimate) + 1) + 1;

                sum += Math.Pow(
                    numOfNodes * nodeSizeEstimate +
                    numOfNonterminals * posSpecificationLengthEstimate +
                    numOfLevelAndNodeSeparators * separatorLength
                    - meanSize,
                    2
                    );
            }

            return Math.Sqrt(sum / (numOfSamples - 1));
        }
    }
}
