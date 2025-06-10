using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Contains the settings of a DummyPointState object. These define how a tree will be generated from it.
    /// </summary>
    internal class DummyPointStateSettings
    {
        /// <summary>
        /// A list of end conditions represented by pairs of functions. The first function determines whether
        /// </summary>
        public List<(Func<DummyPointState, bool>, Func<DummyPointState, bool, double>)> EndConditions { get; }

        public readonly int maxBranchingFactor;

        public readonly int maxDepth;

        public readonly double hitProbability;

        public DummyPointStateSettings(List<(Func<DummyPointState, bool>, Func<DummyPointState, bool, double>)> eConds, int mBF, int mD, double hP) { 
            EndConditions = eConds;
            maxBranchingFactor = mBF;
            maxDepth = mD;
            hitProbability = hP;
        }

    }
}
