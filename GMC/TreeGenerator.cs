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
    /// Defines methods for generating a tree file.
    /// </summary>
    internal class TreeGenerator
    {
        private static string lineSeparator = Environment.NewLine;

        /// <summary>
        /// Generates a file with the specification of a game tree which has the win ratio (WR) and minimax (MM) values computed.
        /// </summary>
        public static void Generate(IGameState root, string outputPath)
        {
            // General outline:
            // 1. Create one file for every level of the tree with only the final level having its WR and MM values fully determined.
            // 2. Go through the created files and compute WR and MM values in the other levels, creating new files in the process.
            // 3. Combine these into one file.
            // 4. Delete the auxiliary files.

            List<long> numOfNonterminalsInLevel = new List<long>();
            List<string> auxFiles = new List<string>();

            string outputDir = Path.GetDirectoryName(outputPath);

            // The try-finally construct is used here to ensure that if an error occurs during
            // this process, the created auxiliary files will be deleted.
            try
            {
                // Create first round of temporary files, one for each level.

                IGameState currentState = root.Clone();
                long numOfNonterminalsInCurrentLevel = 0;

                // Create a file for the root level.
                using (StreamWriter sw = new StreamWriter(outputDir + "\\level_0_A.txt"))
                {
                    numOfNonterminalsInCurrentLevel += WriteState(root, sw, root.GetTeam());
                    sw.WriteLine("/");
                }

                numOfNonterminalsInLevel.Add(numOfNonterminalsInCurrentLevel);
                int levelCounter = 1;
                string prevFile = "level_0_A.txt";
                auxFiles.Add(prevFile);

                // Read the previously created file line by line, load every nonterminal state,
                // create its children and write them down in the new file.
                while (numOfNonterminalsInCurrentLevel != 0)
                {
                    numOfNonterminalsInCurrentLevel = 0;

                    string fileName = "level_" + levelCounter + "_A.txt";
                    auxFiles.Add(fileName);
                    using (StreamReader sr = new StreamReader(outputDir + "\\" + prevFile))
                    using (StreamWriter sw = new StreamWriter(outputDir + "\\" + fileName))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith('N'))
                            {
                                currentState.LoadState(line.Substring(2, line.LastIndexOf('-') - 2));
                                List<IGameState> children = currentState.GetNextGameStates();
                                for (int childIndex = 0; childIndex < children.Count; ++childIndex)
                                {
                                    numOfNonterminalsInCurrentLevel += WriteState(children[childIndex], sw, root.GetTeam());
                                }

                                sw.WriteLine("/");
                            }
                        }
                    }

                    numOfNonterminalsInLevel.Add(numOfNonterminalsInCurrentLevel);
                    ++levelCounter;
                    prevFile = fileName;
                }

                // Go through them and create the second round of intermediate files.

                for (int fileIndex = auxFiles.Count - 2; fileIndex >= 0; --fileIndex)
                {
                    auxFiles.Add("level_" + fileIndex + "_B.txt");

                    using (StreamReader upperLevelReader = new StreamReader(outputDir + "\\" + auxFiles[fileIndex]))
                    using (StreamReader lowerLevelReader = new StreamReader(outputDir + "\\" + auxFiles[auxFiles.Count - 2]))
                    using (StreamWriter sw = new StreamWriter(outputDir + "\\" + "level_" + fileIndex + "_B.txt"))
                    {
                        string upperLine;
                        while ((upperLine = upperLevelReader.ReadLine()) != null)
                        {
                            // If the node is terminal or the line is "/", skip to the next iteration.
                            if (upperLine.StartsWith('T') || upperLine == "/")
                            {
                                sw.WriteLine(upperLine);
                                continue;
                            }

                            double wrValue = 0;
                            long childCount = 0;
                            double minimaxValue = -1;

                            string lowerLine;
                            while ((lowerLine = lowerLevelReader.ReadLine()) != "/")
                            {
                                wrValue += double.Parse(lowerLine.Split(',')[1]);

                                if (minimaxValue == -1)
                                {
                                    minimaxValue = double.Parse(lowerLine.Split(',')[2]);
                                }
                                else if (bool.Parse(upperLine.Split('-')[2]))
                                {
                                    minimaxValue = Math.Max(minimaxValue, double.Parse(lowerLine.Split(',')[2]));
                                }
                                else
                                {
                                    minimaxValue = Math.Min(minimaxValue, double.Parse(lowerLine.Split(',')[2]));
                                }

                                ++childCount;
                            }
                            wrValue /= childCount;

                            sw.WriteLine(upperLine.Split(",")[0] + "," + wrValue + "," + minimaxValue + ",");
                        }
                    }
                }

                // Put it all together into one file.

                long finalFileSizeEstimate = 0;
                for (int bFileIndex = auxFiles.Count - 1; bFileIndex >= (auxFiles.Count + 1) / 2; --bFileIndex)
                {
                    finalFileSizeEstimate += new FileInfo(outputDir + "\\" + auxFiles[bFileIndex]).Length;
                    finalFileSizeEstimate += 1 + lineSeparator.Length;
                }
                long absoluteNumOfDigits = (long)Math.Floor(Math.Log10(finalFileSizeEstimate) + 1) + 3;

                // The starting position of the currently processed file in the final file.
                long currentFileStartPosition = 0;

                long lastOkPosition = -1;
                long prevOffset = -1;
                long lastReaderPosition = -1;
                string lastReaderFile = "";
                long lastOffset = -1;
                long lastNumOfDigits = -1;
                long lastNonTerminalCounter = -1;
                long lastPosition = -1;

                using (FileStream writer = File.Create(outputPath))
                {
                    for (int bFileIndex = auxFiles.Count - 1; bFileIndex >= (auxFiles.Count + 1) / 2 - 1; --bFileIndex)
                    {
                        using (StreamReader currentFileReader = new StreamReader(outputDir + "\\" + auxFiles[bFileIndex]))
                        using (FileStream nextFileReader = File.OpenRead(outputDir + "\\" + auxFiles[bFileIndex - 1]))
                        {
                            // Counts the number of nodes in a level.
                            long numOfNodes = 0;

                            // If this isn't the last file, it means there are some nonterminals, therefore
                            // looking into the next file will be required.
                            long nonTerminalCounter = -1;
                            long positionOffset = -1;
                            if (bFileIndex != (auxFiles.Count + 1) / 2 - 1)
                            {
                                prevOffset = positionOffset;

                                nonTerminalCounter = 0;
                                positionOffset = currentFileStartPosition;
                                positionOffset += currentFileReader.BaseStream.Length + 1 + lineSeparator.Length;
                                positionOffset += numOfNonterminalsInLevel[auxFiles.Count - 1 - bFileIndex] * (absoluteNumOfDigits + 1);
                            }

                            string line;
                            while ((line = currentFileReader.ReadLine()) != null)
                            {
                                // Copy the line from the file.
                                writer.Write(Encoding.ASCII.GetBytes(line));

                                // If the specified node is a nonterminal, add | followed by the position of the first child of
                                // the node defined on that line.
                                if (line.StartsWith('N'))
                                {
                                    writer.WriteByte((byte)'|');

                                    long readerPosition = nextFileReader.Position;
                                    string readerFile = nextFileReader.Name;
                                    long currentPosOffset = positionOffset;
                                    long absNOD = absoluteNumOfDigits;
                                    long curNTC = nonTerminalCounter;

                                    long childrenPosition = GetPositionOfFirstChild(
                                        nextFileReader,
                                        positionOffset,
                                        absoluteNumOfDigits,
                                        nonTerminalCounter,
                                        out nonTerminalCounter
                                        );

                                    if (childrenPosition < lastPosition)
                                    {
                                        Console.WriteLine();
                                        
                                        Console.WriteLine("Previous data:");
                                        Console.WriteLine($"Reader position - {lastReaderPosition}");
                                        Console.WriteLine($"Reader file name - {lastReaderFile}");
                                        Console.WriteLine($"Position offset - {lastOffset}");
                                        Console.WriteLine($"Absolute number of digits - {lastNumOfDigits}");
                                        Console.WriteLine($"Nonterminal counter - {lastNonTerminalCounter}");
                                        Console.WriteLine($"Position in file - {lastPosition}");

                                        Console.WriteLine();

                                        Console.WriteLine($"Current data:");
                                        Console.WriteLine($"Reader position - {readerPosition}");
                                        Console.WriteLine($"Reader file name - {readerFile}");
                                        Console.WriteLine($"Position offset - {currentPosOffset}");
                                        Console.WriteLine($"Absolute number of digits - {absNOD}");
                                        Console.WriteLine($"Nonterminal counter - {curNTC}");
                                        Console.WriteLine($"Position in file - {childrenPosition}");

                                        Console.WriteLine();

                                        throw new ImpossibleException("Children and sibling positions overlap.");
                                    }

                                    lastReaderPosition = readerPosition;
                                    lastReaderFile = readerFile;
                                    lastOffset = currentPosOffset;
                                    lastNumOfDigits = absNOD;
                                    lastNonTerminalCounter = curNTC;
                                    lastPosition = childrenPosition;

                                    long numOfMissingDigits = absoluteNumOfDigits - (long)Math.Floor(Math.Log10(childrenPosition) + 1);

                                    // If numOfMissingDigits is less than zero, it means that there are more digits necessary to
                                    // write down the position than you had anticipated.
                                    if (numOfMissingDigits < 0)
                                    {
                                        throw new Exception("More digits are needed to write down the positions of children in this tree.");
                                    }

                                    for (long zeroCounter = numOfMissingDigits; zeroCounter > 0; --zeroCounter)
                                    {
                                        writer.WriteByte((byte)'0');
                                    }
                                    writer.Write(Encoding.ASCII.GetBytes(childrenPosition.ToString()));
                                }

                                writer.Write(Encoding.ASCII.GetBytes(lineSeparator));

                                if (line != "/")
                                {
                                    ++numOfNodes;
                                }
                            }

                            Console.WriteLine(
                                "Number of nodes at level " +
                                (auxFiles.Count - 1 - bFileIndex) +
                                ": " + numOfNodes
                                );

                            Console.WriteLine("B file size at level " +
                                (auxFiles.Count - 1 - bFileIndex) +
                                ": " + currentFileReader.BaseStream.Length / 1024 + "kB");
                        }

                        writer.WriteByte((byte)'#');
                        writer.Write(Encoding.ASCII.GetBytes(lineSeparator));

                        currentFileStartPosition = writer.Length;
                    }

                    writer.Flush();
                }
            }
            finally
            {
                // Delete the auxiliary files.

                foreach (var file in auxFiles)
                {
                    File.Delete(outputDir + "\\" + file);
                }
            }
        }

        /// <summary>
        /// Writes a representation of the given state using the given stream writer.
        /// The team parameter determines the team from the perspective of which the
        /// score of terminal nodes is to be written down.
        /// </summary>
        /// <returns> 0 if the state was terminal, 1 otherwise. </returns>
        private static int WriteState(IGameState state, StreamWriter sw, bool team)
        {
            if (state.IsTerminal())
            {
                sw.Write("T-" + state.ToString());
                double val = state.GetScore(team);
                sw.WriteLine("-," + val + "," + val + ",");

                return 0;
            }
            else
            {
                sw.Write("N-" + state.ToString());
                sw.WriteLine("-,X,X,");

                return 1;
            }
        }

        /// <summary>
        /// Gets the position of the next first child in a file.
        /// </summary>
        /// <param name="reader"> The stream that reads the file to be processed. </param>
        /// <param name="positionOffset"> The position of the start of the given file's contents in the final file. </param>
        /// <param name="absNumOfDigits"> The number of digits required to specify the position in the final file. </param>
        /// <param name="nonTerminalCounterStart"> The number of nonterminals encountered thus far. </param>
        /// <param name="nonTerminalCounter"> The number of nonterminals encountered at the end of the computation. </param>
        private static long GetPositionOfFirstChild
            (
            FileStream reader,
           long positionOffset,
           long absNumOfDigits,
           long nonTerminalCounterStart,
           out long nonTerminalCounter)
        {             
            nonTerminalCounter = nonTerminalCounterStart;

            // The final position is the position of the start of the file being read in the final file
            // plus the current position of the reader plus the number of characters necessary to specify the
            // addresses of the children of all the nonterminal nodes encountered thus far.
            long result = positionOffset + reader.Position + nonTerminalCounter * (absNumOfDigits + 1);

            // Shift the position of the reader to the start of the next node's children.
            // While doing that, count the encountered nonterminals as they
            // will have the position of their first child added to them.
            int b;
            while((b = reader.ReadByte()) != (byte)'/')
            {
                if (b == (byte)'N')
                {
                    nonTerminalCounter++;
                }
            }

            // Absorb the line separator.
            for (int i = 0; i < lineSeparator.Length; ++i)
            {
                reader.ReadByte();
            }

            return result;
        }
    }
}
