using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Interface that defines the methods of a game state representation.
    /// </summary>
    internal interface IGameState
    {
        bool IsTerminal();

        /// <summary>
        /// Get the minimax evaluation of a given state from the given team's point of view.
        /// </summary>
        double GetScore(bool team);

        /// <summary>
        /// Get the team whose turn it currently is.
        /// </summary>
        bool GetTeam();

        /// <summary>
        /// Get game state to which one can get from this one.
        /// </summary>
        List<IGameState> GetNextGameStates();

        /// <summary>
        /// Loads the given state definition into the current state object,
        /// overwriting its data.
        /// </summary>
        /// <param name="definition"> The string representation of the state to be loaded. </param>
        void LoadState(string definition);

        IGameState Clone();

        /// <summary>
        /// Returns an object capable of calculating the probability that a node at the current level will be terminal.
        /// </summary>
        /// <returns></returns>
        ITerminationProbabilityCalculator GetTPC();

        /// <summary>
        /// Returns a string representation of the state. The representation should always
        /// have the same length - otherwise, the TreeGenerator may not be able to work with it.
        /// </summary>
        string ToString();
    }
}
