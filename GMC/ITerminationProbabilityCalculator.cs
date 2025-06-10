using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Interface for an object that calculates the termination probability at a given level in a generated tree.
    /// For every state file, one of these should exist so that it is possible to estimate the size
    /// of resulting files.
    /// </summary>
    internal interface ITerminationProbabilityCalculator
    {
        /// <summary>
        /// Incrementally calculates the termination probability at the current level.
        /// </summary>
        public double CalculateCurrentTerminationProbability();

        /// <summary>
        /// Resets the internal values so that the computation can be restarted from the beginning.
        /// </summary>
        public void Reset();
    }
}
