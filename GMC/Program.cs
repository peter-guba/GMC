using Microsoft.VisualBasic;
using GMC;
using GMC.Exceptions;
using GMC.SearchAlgorithms;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

class Program
{
    static void Main(string[] args)
    {
        // Generate a tree file.
        if (args[0] == "--generate" || args[0] == "-g")
        {
            int maxBranchingFactor = int.Parse(args[1]);
            int maxDepth = int.Parse(args[2]);
            int whitePoints = int.Parse(args[3]);
            int blackPoints = int.Parse(args[4]);
            double hitProbability = double.Parse(args[5]);
            int seed = int.Parse(args[6]);
            string outputPath = args[7];

            // Initialize the root state.

            Func<DummyPointState, bool> endCheck = (state) => state.WhitePoints == 0 || state.BlackPoints == 0;
            Func<DummyPointState, bool, double> getScore = (state, team) =>
            {
                if (state.WhitePoints == 0)
                {
                    return team ? 0 : 1;
                }
                else if (state.BlackPoints == 0)
                {
                    return team ? 1 : 0;
                }

                throw new Exception("This node isn't terminal, score can't be assigned.");
            };

            DummyPointState.rand = new Random(seed);

            IGameState root = new DummyPointState(
                new DummyPointStateSettings(
                    new List<(Func<DummyPointState, bool>, Func<DummyPointState, bool, double>)> { (endCheck, getScore) },
                    maxBranchingFactor,
                    maxDepth,
                    hitProbability
                    ),
                whitePoints,
                blackPoints,
                true
                );

            //Generate tree.

            TreeGenerator.Generate(root, outputPath);
        }
        // Measure the convergence of an algorithm on a given tree file.
        else if (args[0] == "--measure" || args[0] == "-m")
        {
            string algorithmName = ExtractArgValues(args[1], args[2]);
            ISearch algorithm = AuxiliaryFunctions.GetAlgorithm(algorithmName);
            string sourceFile = args[2];
            int numOfIterations = int.Parse(args[3]);
            bool perNode = bool.Parse(args[4]);
            int numOfRepeats = int.Parse(args[5]);
            RewardType rtype = AuxiliaryFunctions.GetRewardType(args[6], 0, out _);
            bool bestOnly = bool.Parse(args[7]);
            bool moreThanOneChildNecessary = bool.Parse(args[8]);
            string outputPath = args[9];

            Random rand = new Random(Guid.NewGuid().GetHashCode());

            for (int repeatsCounter = 0; repeatsCounter < numOfRepeats; ++repeatsCounter)
            {
                string dateAndTime = DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");

                // Gets the name of the directory that contains the source file.
                // (The name of that directory is the category of the file.)
                string dirName = Path.GetFileName(Path.GetDirectoryName(sourceFile)) + "_";

                ConvergenceMeasurer.MeasureConvergence(
                    algorithm,
                    sourceFile,
                    numOfIterations,
                    perNode,
                    rtype,
                    outputPath + "/" + dirName + Path.GetFileNameWithoutExtension(sourceFile) + "_" + numOfIterations + "_" + dateAndTime + "_" + repeatsCounter + "_" + rand.Next(10000) + ".txt",
                    bestOnly,
                    moreThanOneChildNecessary
                    );
            }
        }
        // Crunch the data for a given number of iterations and tree size.
        else if (args[0] == "--crunch" || args[0] == "-c")
        {
            if (args[1] == "--pattern" || args[1] == "-p")
            {
                bool bestChoiceFlagPresent = bool.Parse(args[2]);
                string regex = args[3];
                string inputPath = args[4];
                string outputPath = args[5];

                DataCruncher.CrunchItAll(bestChoiceFlagPresent, inputPath, outputPath, regex);
            }
            else
            {
                bool bestChoiceFlagPresent = bool.Parse(args[1]);
                int numOfIters = int.Parse(args[2]);
                string treeFileName = args[3];
                string inputPath = args[4];
                string outputPath = args[5];

                DataCruncher.CrunchItAll(bestChoiceFlagPresent, inputPath, outputPath, treeFileName + "_" + numOfIters + "*");
            }
        }
        // Estimate the size of a file generated using the given settings.
        else if (args[0] == "--estimate" || args[0] == "-e")
        {
            int maxBranchingFactor = int.Parse(args[1]);
            int whitePoints = int.Parse(args[2]);
            int blackPoints = int.Parse(args[3]);
            double hitProb = double.Parse(args[4]);
            int maxDepth = int.Parse(args[5]);
            int numOfSamples = int.Parse(args[6]);

            DummyPointState origin = new DummyPointState(
                new DummyPointStateSettings(
                    null,
                    maxBranchingFactor,
                    maxDepth,
                    hitProb
                ),
                whitePoints, blackPoints, true
            );

            int nodeSizeEstimate = "X-".Length + origin.ToString().Length + ",0.0000000000000000,1,\n".Length;

            double fileMeanSizeEstimate = SizeEstimator.EstimateMeanFileSize(
                maxBranchingFactor,
                3,
                origin.GetTPC(),
                maxDepth,
                nodeSizeEstimate
                );

            Console.WriteLine("Mean file size estimate: " + AuxiliaryFunctions.GetSizeInProperUnits(fileMeanSizeEstimate));

            double fileSizeSDEstimate = SizeEstimator.EstimateFileSizeStandardDeviation(
                fileMeanSizeEstimate,
                numOfSamples,
                maxBranchingFactor,
                3,
                origin.GetTPC(),
                maxDepth,
                nodeSizeEstimate
                );

            Console.WriteLine("File size standard deviation estimate: " + AuxiliaryFunctions.GetSizeInProperUnits(fileSizeSDEstimate));
            Console.WriteLine(
                "95% probability file size normal range estimate: " +
                AuxiliaryFunctions.GetSizeInProperUnits(fileMeanSizeEstimate - 1.96 * fileSizeSDEstimate) + ", " +
                AuxiliaryFunctions.GetSizeInProperUnits(fileMeanSizeEstimate + 1.96 * fileSizeSDEstimate)
                );
        }
        // Check if a given tree was correctly generated.
        else if (args[0] == "--check" || args[0] == "-h")
        {
            TreeChecker.CheckTree(args[1]);
        }
        // Check if the tree specified in a given file is deceptive.
        else if (args[0] == "--deceptionCheck" || args[0] == "-d")
        {
            string file = args[1];
            Console.WriteLine(DeceptionChecker.IsDeceptive(file));
        }
        else if (args[0] == "--help")
        {
            Console.WriteLine(
                @"--check, -h [path to tree file]
	Checks if a given tree has been generated correctly (i.e. that all the children positions specified in the file lead to valid positions).
--crunch, -c [--pattern/-p, best choice flag present, regex, input path, output path] / [best choice flag present, number of iterations, tree file name, input path, output path]
	Computes the mean and variance of data from files that match a given regex. If the --pattern option isn't specified, the regex is constructed to fit a tree with the given name and given number of iterations.
--deceptionCheck, -d [file to check]
	Checks if the tree stored in the given file is deceptive, i.e. the node with the highest win ratio doesn't have the highest minimax value.
--estimate, -e [maximum branching factor, first player's points, second player's points, hit probability, maximum depth, number of samples]
	!!!THIS COMMAND ISN'T FULLY DEBUGGED YET AND SOMETIMES RETURNS WRONG ESTIMATES!!!
	Computes estimates of the mean size of trees generated using the given parameters, their standard deviation and 95% confidence interval.
--generate, -g [maximum branching factor, maximum depth, first player's points, second player's points, hit probability, seed, output path]
	Randomly generates a tree for a two-player zero-sum game using the given parameters.
--help []
    Display a help message.
--measure, -m [algorithm name, path to tree file, number of iterations, per node, number of repeats, reward type, best only, more than one child necessary, output path]
	Runs a given algorithm on the tree specified in the given file for a given number of iterations and repeats the process a given number of times.


If you need more detailed descriptions of the commands and their parameters, please take a look at the documentation at https://github.com/peter-guba/GMC/tree/master."
            );
        }
    }

    /// <summary>
    /// Extracts argument values such as maximum depth or branching value of the input
    /// tree from the input file name.
    /// </summary>
    private static string ExtractArgValues(string algorithmString, string inputPath)
    {
        string[] aNParts = algorithmString.Split('_');
        string fileName = Path.GetFileName(inputPath);
        string result = aNParts[0];

        for (int aNPIndex = 1; aNPIndex < aNParts.Length; ++aNPIndex)
        {
            result += "_";

            switch (aNParts[aNPIndex])
            {
                case "@d": result += fileName.Split('_')[2]; break;
                default: result += aNParts[aNPIndex]; break;
            }
        }

        return result;
    }
}