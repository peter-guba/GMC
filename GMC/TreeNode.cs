using GMC.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Different reward types that can be used during computations.
    /// </summary>
    internal enum RewardType
    {
        WinRatio, // The probability of ending up in a winning state by making random moves from a given starting state.
        Minimax, // The value of a result (1 = win, 0.5 = draw, 0 = loss) obtained if both players played perfectly from a given starting state.
        RandomNoiseWREstimate, // WinRatio with added random noise to simulate imperfection of heuristic computations.
        GradualChangeWREstimate // WinRatio with added noise that changes at most by a given amount to simulate that heuristic
                                // values usually don't change two wildly between consecutive states.
    }

    /// <summary>
    /// Represents a single node of a game tree.
    /// </summary>
    internal class TreeNode
    {
        /// <summary>
        /// A FileStream that reads a file containing the description of a tree.
        /// </summary>
        public static FileStream reader;

        /// <summary>
        /// Determines the level of possible noise when computing RandomNoiseWREstimate.
        /// </summary>
        public static double noiseMultiplier = 0.5;

        /// <summary>
        /// Determines the initial deviation of GradualChangeWREstimate from the true
        /// WinRatio. If adding this number to WinRatio would result in overflow, its
        /// sign is flipped.
        /// </summary>
        public static double initialNoise = 0.5;

        /// <summary>
        /// Determines the maximum difference between the WR estimates of a parent node and
        /// a child node when GradualChangeWREstimate is used.
        /// </summary>
        public static double maxNoiseStep = 0.1;

        /// <summary>
        /// The player whose turn it is at the beginning.
        /// </summary>
        private static bool initialPlayer;

        private static Random rand;

        public TreeNode Parent { get; private set; }

        public bool ChildrenLoaded { get; private set; }

        public List<TreeNode> Children { get; set; }

        public double WinRatio { get; private set; }

        public double RNWREstimate { get; private set; }

        public double GCWREstimate { get; private set; }

        public double MinimaxValue { get; private set; }

        public bool CurrentPlayer { get; private set; }

        public readonly bool isTerminal;

        /// <summary>
        /// The position of the first character of the node's first child in the file that is being
        /// loaded.
        /// </summary>
        public long childrenStartPosition;

        /// <summary>
        /// The index of the next child to be returned.
        /// </summary>
        private int nextChildIndex = 0;

        /// <summary>
        /// Parses a line of input from the tree file that the reader has open. isTerminal is given by a
        /// parameter because the first character of the specification of a node is loaded beforehand
        /// and used to determine if there is another node available.
        /// </summary>
        private TreeNode(TreeNode parent, bool isTerminal)
        {
            Parent = parent;
            Children = new List<TreeNode>();
            ChildrenLoaded = false;

            // Load the data.
            this.isTerminal = isTerminal;
            ReadUpToDelimiter('-');
            CurrentPlayer = bool.Parse(ReadUpToDelimiter('-'));
            if (parent == null)
            {
                initialPlayer = CurrentPlayer;
                rand = new Random(Guid.NewGuid().GetHashCode());
            }
            ReadUpToDelimiter(',');
            WinRatio = double.Parse(ReadUpToDelimiter(','));
            MinimaxValue = double.Parse(ReadUpToDelimiter(','));
            RNWREstimate = double.NaN;
            GCWREstimate = double.NaN;

            if (!isTerminal)
            {
                reader.ReadByte();
                childrenStartPosition = long.Parse(ReadUpToDelimiter('\n'));
            }
            else
            {
                ReadUpToDelimiter('\n');
            }
        }

        /// <summary>
        /// Returns the reward of a given type at this node for a given player.
        /// </summary>
        public double GetRewardFor(bool player, RewardType rtype)
        {
            double reward;

            switch (rtype)
            {
                case RewardType.WinRatio: reward = WinRatio; break;
                case RewardType.Minimax: reward = MinimaxValue; break;
                case RewardType.RandomNoiseWREstimate:
                    {
                        if (double.IsNaN(RNWREstimate))
                        {
                            if (isTerminal)
                            {
                                RNWREstimate = MinimaxValue;
                            }
                            else
                            {
                                RNWREstimate = Math.Min(1, Math.Max(0, WinRatio + noiseMultiplier * (rand.NextDouble() * 2 - 1)));
                            }
                        }
                        reward = RNWREstimate;
                        break;
                    }
                case RewardType.GradualChangeWREstimate:
                    {
                        if (double.IsNaN(GCWREstimate))
                        {
                            if (isTerminal)
                            {
                                GCWREstimate = MinimaxValue;
                            }
                            else
                            {
                                if (Parent == null)
                                {
                                    GCWREstimate = Math.Min(1, Math.Max(0, WinRatio + initialNoise));
                                }
                                else
                                {
                                    double parentEstimate = Parent.GetRewardFor(initialPlayer, RewardType.GradualChangeWREstimate);

                                    if (WinRatio > Parent.WinRatio)
                                    {
                                        GCWREstimate = Math.Min(1, parentEstimate + maxNoiseStep * rand.NextDouble());
                                    }
                                    else if (WinRatio < Parent.WinRatio)
                                    {
                                        GCWREstimate = Math.Max(0, parentEstimate - maxNoiseStep * rand.NextDouble());
                                    }
                                    else
                                    {
                                        GCWREstimate = Math.Min(1, Math.Max(0, parentEstimate + maxNoiseStep * (rand.NextDouble() * 2 - 1)));
                                    }
                                }
                            }
                        }
                        reward = GCWREstimate;
                        break;
                    }
                default: throw new ImpossibleException("An unknown reward type - this shouldn't be possible.");
            }

            if (player == initialPlayer)
            {
                return reward;
            }
            else
            {
                return 1 - reward;
            }
        }

        public TreeNode GetRandomChild()
        {
            if (isTerminal)
            {
                throw new IllegalOperationException("Random child cannot be returned for terminal node.");
            }

            if (!ChildrenLoaded)
            {
                LoadChildren();
            }

            // If the node isn't terminal but has no children even after calling LoadChildren, something is wrong.
            if (Children.Count == 0)
            {
                Console.WriteLine("Is node terminal?: " + isTerminal);
                Console.WriteLine("File path: " + reader.Name);

                Console.WriteLine("Node start position: " + Parent.childrenStartPosition);
                reader.Seek(Parent.childrenStartPosition - 145, SeekOrigin.Begin);

                // Print 290 characters around the current node.
                for (int counter = 0; counter < 290; ++counter)
                {
                    Console.Write((char)reader.ReadByte());
                }
                Console.WriteLine();

                Console.WriteLine("Children start position: " + childrenStartPosition);
                reader.Seek(childrenStartPosition - 145, SeekOrigin.Begin);

                // Print 290 characters around the position where the node's children should start.
                for (int counter = 0; counter < 290; ++counter)
                {
                    Console.Write((char)reader.ReadByte());
                }
                Console.WriteLine();

                throw new ImpossibleException("Nonterminal node with no children found.");
            }

            return Children[rand.Next(Children.Count)];
        }

        public TreeNode GetNextChild()
        {
            if (isTerminal)
            {
                throw new IllegalOperationException("Child cannot be returned for terminal node.");
            }

            if (!ChildrenLoaded)
            {
                LoadChildren();
            }

            if (nextChildIndex == Children.Count)
            {
                return null;
            }

            return Children[nextChildIndex++];
        }

        /// <summary>
        /// Uses the reader to read bytes until a given delimiter is found and returns them as a string.
        /// </summary>
        private string ReadUpToDelimiter(char delim)
        {
            string result = "";
            int currentByte = reader.ReadByte();

            while (currentByte != delim)
            {
                result += (char)currentByte;
                currentByte = reader.ReadByte();
            }

            return result;
        }

        /// <summary>
        /// Loads the children of this node from the file that reader is reading.
        /// </summary>
        public void LoadChildren()
        {
            if (ChildrenLoaded)
            {
                return;
            }

            reader.Seek(childrenStartPosition, SeekOrigin.Begin);
            
            int firstByte = reader.ReadByte();
            while (firstByte == 'T' || firstByte == 'N')
            {
                Children.Add(new TreeNode(this, firstByte == 'T'));
                firstByte = reader.ReadByte();
            }

            ChildrenLoaded = true;
        }

        public int GetNumberOfChildren()
        {
            if (!ChildrenLoaded)
            {
                LoadChildren();
            }

            return Children.Count;
        }

        public static TreeNode GetRoot(string path)
        {
            reader = File.OpenRead(path);
            return new TreeNode(null, reader.ReadByte() == 'T');
        }
    }
}
