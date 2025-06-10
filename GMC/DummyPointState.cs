using GMC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// A state with two players, each of whom has some points. The players take turns trying to
    /// destroy the opponent's points. The resulting tree is bounded by a maximum branching factor
    /// and maximum depth, the former of which diminishes with depth.
    /// </summary>
    internal class DummyPointState : IGameState
    {
        private DummyPointStateSettings settings;

        /// <summary>
        /// The number of points the white player has left.
        /// </summary>
        public int WhitePoints { get; private set; }

        /// <summary>
        /// The number of points the black player has left.
        /// </summary>
        public int BlackPoints { get; private set; }

        /// <summary>
        /// The current depth of this state in the game tree (can't exceed a value given
        /// in the settings).
        /// </summary>
        private int depth;

        /// <summary>
        /// The team of the player whose move it currently is.
        /// true <=> white.
        /// </summary>
        private bool team;

        /// <summary>
        /// Determines whether the team in this node is the same as the player.
        /// </summary>
        private bool maximizing;

        /// <summary>
        /// If the node is terminal, this variable is set to the index of the condition that determined its end.
        /// </summary>
        private int winConditionIndex = -1;

        public static Random rand;

        public DummyPointState(
            DummyPointStateSettings settings,
            int whitePoints,
            int blackPoints,
            bool team
            )
        {       
            this.settings = settings;

            WhitePoints = whitePoints;
            BlackPoints = blackPoints;

            this.team = team;
            maximizing = true;
            depth = 0;
        }

        private DummyPointState(
            DummyPointStateSettings settings,
            int whitePoints,
            int blackPoints,
            int depth,
            bool team,
            bool maximizing
            )
        {
            this.settings = settings;

            WhitePoints = whitePoints;
            BlackPoints = blackPoints;

            this.depth = depth;
            this.team = team;
            this.maximizing = maximizing;
        }

        /// <summary>
        /// Checks all the end conditions given in the settings to see if the state is terminal.
        /// Also checks if the maximum depth has been reached.
        /// </summary>
        public bool IsTerminal()
        {
            // If the node has already been determined to be terminal,
            // return true.
            if (winConditionIndex != -1)
            {
                return true;
            }
            
            // First check all the terminal conditions.
            for (int condIndex = 0; condIndex < settings.EndConditions.Count; ++condIndex)
            {
                if (settings.EndConditions[condIndex].Item1(this))
                {
                    winConditionIndex = condIndex;
                    return true;
                }
            }

            // If no terminal conditions were satisfied, check if max depth has been reached.
            if (depth == settings.maxDepth)
            {
                winConditionIndex = settings.EndConditions.Count;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the team of the player whose turn it is in this state.
        /// true <=> white
        /// </summary>
        public bool GetTeam()
        {
            return team;
        }

        /// <summary>
        /// If the node is terminal, this returns a score signifying whether the state is a win or a loss
        /// for a given player. Otherwise, it throws an exception.
        /// </summary>
        public double GetScore(bool team)
        {
            // If the winConditionIndex is set to -1, it either means that the IsTerminal method hasn't been called
            // yet, or that the node isn't terminal.
            if (winConditionIndex == -1)
            {
                if (!IsTerminal())
                {
                    throw new IllegalOperationException("This node isn't terminal, so it has no score.");
                }
            }

            // If the winConditionIndex is set to settings.EndConditions.Count, it means that the maximum depth has been
            // reached, in which case the game is considered a draw.
            if (winConditionIndex == settings.EndConditions.Count)
            {
                return 0.5f;
            }

            return settings.EndConditions[winConditionIndex].Item2(this, team);
        }

        /// <summary>
        /// Creates a random number of children. The upper bound starts at settings.maxBranchingFactor, but
        /// progressively changes to 3 with increasing depth. In each child, the number of points of the
        /// opponent will be decreased with probability settings.hitProbability.
        /// </summary>
        public List<IGameState> GetNextGameStates()
        {
            if (IsTerminal())
            {
                throw new IllegalOperationException("Child cannot be created for a terminal node.");
            }

            // Decrease the branching factor with depth.
            int branchingFactorBound = ((settings.maxDepth - depth - 1) * settings.maxBranchingFactor + depth * 3) / (settings.maxDepth - 1);
            int numOfChildren = rand.Next(branchingFactorBound) + 1;
            List<IGameState> children = new List<IGameState>();

            for (int childrenIndex = 0; childrenIndex < numOfChildren; ++childrenIndex)
            {
                int newWhitePoints = WhitePoints;
                int newBlackPoints = BlackPoints;
                if (rand.NextDouble() < settings.hitProbability) {
                    if (team)
                    {
                        --newBlackPoints;
                    }
                    else
                    {
                        --newWhitePoints;
                    }
                }

                children.Add(new DummyPointState(
                    settings,
                    newWhitePoints,
                    newBlackPoints,
                    depth + 1,
                    !team,
                    !maximizing
                    ));
            }

            return children;
        }

        public void LoadState(string specification)
        {
            string[] fields = specification.Split('-');
            team = bool.Parse(fields[0]);
            maximizing = bool.Parse(fields[1]);
            WhitePoints = int.Parse(fields[2]);
            BlackPoints = int.Parse(fields[3]);
            depth = int.Parse(fields[4]);
        }

        public IGameState Clone()
        {
            return new DummyPointState(settings, WhitePoints, BlackPoints, depth, team, maximizing);
        }

        public double GetHitProbability()
        {
            return settings.hitProbability;
        }

        public ITerminationProbabilityCalculator GetTPC()
        {
            return new DummyPointStateTPC(this);
        }

        public override string ToString()
        {
            return team + "-" + maximizing + "-" + WhitePoints + "-" + BlackPoints + "-" + depth;
        }
    }
}
