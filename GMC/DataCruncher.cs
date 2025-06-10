using GMC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Defiines methods for computing the mean and variance of given data.
    /// </summary>
    internal class DataCruncher
    {
        /// <summary>
        /// The maximum number of input streams that the program can have open at a time.
        /// Note that one of the streams is reserved for an auxiliary file, so if you want
        /// the program to compute the average of n files at one time, this value has to
        /// be set to n+1.
        /// </summary>
        public static int inputsLimit = 1001;

        /// <summary>
        /// Takes files created by the ConvergenceMeasurer and computes averages of the values in them.
        /// </summary>
        /// <param name="bestChoiceFlagPresent"> Determines whether the provided data contain the best
        /// choice flag (which determines whether the best node was chosen in the given iteration)
        /// or not. </param>
        /// <param name="inputPath"> The path to the input files. </param>
        /// <param name="outputPath"> The output path (including the name of the output file). </param>
        /// <param name="fileSelector"> A regex used to select files in the input folder. </param>
        /// <exception cref="IllegalOperationException"> Thrown if no files that match the given
        /// regex are found (so also thrown when the inputs folder is empty). </exception>
        public static void CrunchItAll(bool bestChoiceFlagPresent, string inputPath, string outputPath, string fileSelector)
        {
            DirectoryInfo di = new DirectoryInfo(inputPath);
            List<FileInfo> files = new List<FileInfo>(di.GetFiles(fileSelector, SearchOption.TopDirectoryOnly));

            CrunchItAll(bestChoiceFlagPresent, true, outputPath, files);
        }

        /// <summary>
        /// An override of the CrunchItAll method that takes a list of files as input instead of an input path
        /// and a regex for selecting from the given files.
        /// </summary>
        public static void CrunchItAll(bool bestChoiceFlagPresent, bool computeVariance, string outputPath, List<FileInfo> files)
        {
            if (files.Count == 0)
            {
                throw new IllegalOperationException("No files supplied to crunch.");
            }

            List<string> auxiliaryFilePaths = new List<string>();
            
            int processedFilesCount = 0;
            while (processedFilesCount != files.Count)
            {
                // If the number of input files is large, the files are processed in batches, the size of which
                // is determined by the inputsLimit variable. Several auxiliary files are created in this process,
                // which are then deleted. So, first, the first n files are averaged and the result is stored
                // in a temporary file called "_aux{i}" where i is a number. Then, the average of the next n
                // files and the previous auxiliary file are created, etc., until all files are processed.

                string output;
                // If a all the remaining files can be processed in this batch, create the final file.
                if (files.Count - processedFilesCount <= inputsLimit - 1)
                {
                    output = outputPath;
                }
                // Otherwise, create an auxiliary file that will be deleted later.
                else
                {
                    output = Path.GetDirectoryName(outputPath) + "/_aux" + (processedFilesCount / (inputsLimit - 1)) + ".txt";
                }

                using (StreamWriter sw = new StreamWriter(output))
                {
                
                    List<FileStream> inputStreams = new List<FileStream>();

                    for (int index = 0; index < (inputsLimit - 1) && index + processedFilesCount < files.Count; ++index)
                    {
                        inputStreams.Add(File.OpenRead(files[index + processedFilesCount].FullName));
                    }

                    if (auxiliaryFilePaths.Count != 0)
                    {
                        inputStreams.Add(File.OpenRead(auxiliaryFilePaths[auxiliaryFilePaths.Count - 1]));
                    }

                    bool allFilesOpen = true;

                    // If the end of one of the files is encountered, stop going through the files.
                    // That way, the resulting file will have the same number of entries as the file
                    // with the least entries.
                    while (allFilesOpen)
                    {
                        double bestChoiceFlag = 0;
                        double oneOfBestFlag = 0;
                        if (bestChoiceFlagPresent)
                        {
                            foreach (FileStream fs in inputStreams)
                            {
                                double val = GetNumericValue(fs, '|');
                                if (double.IsNaN(val))
                                {
                                    allFilesOpen = false;
                                    break;
                                }

                                if (Path.GetFileName(fs.Name).StartsWith("_aux"))
                                {
                                    bestChoiceFlag += val * processedFilesCount;
                                }
                                else
                                {
                                    bestChoiceFlag += val;
                                }
                            }

                            foreach (FileStream fs in inputStreams)
                            {
                                double val = GetNumericValue(fs, '|');
                                if (double.IsNaN(val))
                                {
                                    allFilesOpen = false;
                                    break;
                                }

                                if (Path.GetFileName(fs.Name).StartsWith("_aux"))
                                {
                                    oneOfBestFlag += val * processedFilesCount;
                                }
                                else
                                {
                                    oneOfBestFlag += val;
                                }
                            }
                        }

                        double currentValue = 0;
                        foreach (FileStream fs in inputStreams)
                        {
                            double val = GetNumericValue(fs, ',');
                            if (double.IsNaN(val))
                            {
                                allFilesOpen = false;
                                break;
                            }

                            if (Path.GetFileName(fs.Name).StartsWith("_aux"))
                            {
                                currentValue += val * processedFilesCount;
                            }
                            else
                            {
                                currentValue += val;
                            }
                        }

                        if (allFilesOpen)
                        {
                            if (processedFilesCount == 0)
                            {
                                currentValue /= inputStreams.Count;
                                bestChoiceFlag /= inputStreams.Count;
                                oneOfBestFlag /= inputStreams.Count;
                            }
                            else
                            {
                                currentValue /= processedFilesCount + inputStreams.Count - 1;
                                bestChoiceFlag /= processedFilesCount + inputStreams.Count - 1;
                                oneOfBestFlag /= processedFilesCount + inputStreams.Count - 1;
                            }

                            string dataToWrite = "";
                            if (bestChoiceFlagPresent)
                            {
                                dataToWrite += bestChoiceFlag + "|" + oneOfBestFlag + "|" + currentValue;
                            }
                            else
                            {
                                dataToWrite += currentValue;
                            }

                            sw.Write(dataToWrite + ",");
                        }
                    }

                    foreach (FileStream fs in inputStreams)
                    {
                        fs.Close();
                    }

                    if (processedFilesCount == 0)
                    {
                        processedFilesCount += inputStreams.Count;
                    }
                    else
                    {
                        processedFilesCount += inputStreams.Count - 1;
                    }

                    if (Path.GetFileName(output).StartsWith("_aux"))
                    {
                        auxiliaryFilePaths.Add(output);
                    }
                }
            }

            foreach (string filePath in auxiliaryFilePaths)
            {
                File.Delete(filePath);
            }

            // Now that the means have been computed, variances can be computed in a separate file.

            if (computeVariance)
            {
                List<string> auxiliaryVarFilePaths = new List<string>();

                int processedVarFilesCount = 0;
                while (processedVarFilesCount != files.Count)
                {
                    string std_err_output;
                    // If a all the remaining files can be processed in this batch, create the final file.
                    if (files.Count - processedVarFilesCount <= inputsLimit - 1)
                    {
                        // Remove the ".txt" suffix before adding "_variance" to the end of the file name.
                        std_err_output = outputPath.Substring(0, outputPath.Length - 4) + "_var.txt";
                    }
                    // Otherwise, create an auxiliary file that will be deleted later.
                    else
                    {
                        std_err_output = Path.GetDirectoryName(outputPath) + "/_var_aux" + (processedVarFilesCount / (inputsLimit - 1)) + ".txt";
                    }

                    using (StreamWriter sw = new StreamWriter(std_err_output))
                    using (FileStream meansReader = File.OpenRead(outputPath))
                    {

                        List<FileStream> inputStreams = new List<FileStream>();

                        for (int index = 0; index < (inputsLimit - 1) && index + processedVarFilesCount < files.Count; ++index)
                        {
                            inputStreams.Add(File.OpenRead(files[index + processedVarFilesCount].FullName));
                        }

                        if (auxiliaryVarFilePaths.Count != 0)
                        {
                            inputStreams.Add(File.OpenRead(auxiliaryVarFilePaths[auxiliaryVarFilePaths.Count - 1]));
                        }

                        bool allFilesOpen = true;

                        // If the end of one of the files is encountered, stop going through the files.
                        // That way, the resulting file will have the same number of entries as the file
                        // with the least entries.
                        while (allFilesOpen)
                        {
                            double bCFMean = -1;
                            double oOBFMean = -1;
                            if (bestChoiceFlagPresent)
                            {
                                bCFMean = GetNumericValue(meansReader, '|');
                                oOBFMean = GetNumericValue(meansReader, '|');
                            }

                            double mean = GetNumericValue(meansReader, ',');

                            double bestChoiceFlagVar = 0;
                            double oneOfBestFlagVar = 0;
                            if (bestChoiceFlagPresent)
                            {
                                foreach (FileStream fs in inputStreams)
                                {
                                    double val = GetNumericValue(fs, '|');
                                    if (double.IsNaN(val))
                                    {
                                        allFilesOpen = false;
                                        break;
                                    }

                                    if (Path.GetFileName(fs.Name).StartsWith("_var_aux"))
                                    {
                                        bestChoiceFlagVar += val * (processedVarFilesCount - 1);
                                    }
                                    else
                                    {
                                        bestChoiceFlagVar += (val - bCFMean) * (val - bCFMean);
                                    }
                                }

                                foreach (FileStream fs in inputStreams)
                                {
                                    double val = GetNumericValue(fs, '|');
                                    if (double.IsNaN(val))
                                    {
                                        allFilesOpen = false;
                                        break;
                                    }

                                    if (Path.GetFileName(fs.Name).StartsWith("_var_aux"))
                                    {
                                        oneOfBestFlagVar += val * (processedVarFilesCount - 1);
                                    }
                                    else
                                    {
                                        oneOfBestFlagVar += (val - oOBFMean) * (val - oOBFMean);
                                    }
                                }
                            }

                            double currentVar = 0;
                            foreach (FileStream fs in inputStreams)
                            {
                                double val = GetNumericValue(fs, ',');
                                if (double.IsNaN(val))
                                {
                                    allFilesOpen = false;
                                    break;
                                }

                                if (Path.GetFileName(fs.Name).StartsWith("_var_aux"))
                                {
                                    currentVar += val * (processedVarFilesCount - 1);
                                }
                                else
                                {
                                    currentVar += (val - mean) * (val - mean);
                                }
                            }

                            if (allFilesOpen)
                            {
                                if (processedVarFilesCount == 0)
                                {
                                    currentVar /= inputStreams.Count - 1;
                                    bestChoiceFlagVar /= inputStreams.Count - 1;
                                    oneOfBestFlagVar /= inputStreams.Count - 1;
                                }
                                else
                                {
                                    // The -2 factor at the end conists of two -1. One to subtract the file that
                                    // contains averaged values, the other comes from the fact that, to compute
                                    // the standard deviation, you divide by n-1, not n.
                                    currentVar /= processedVarFilesCount + inputStreams.Count - 2;
                                    bestChoiceFlagVar /= processedVarFilesCount + inputStreams.Count - 2;
                                    oneOfBestFlagVar /= processedVarFilesCount + inputStreams.Count - 2;
                                }

                                string dataToWrite = "";
                                if (bestChoiceFlagPresent)
                                {
                                    dataToWrite += bestChoiceFlagVar + "|" + oneOfBestFlagVar + "|" + currentVar;
                                }
                                else
                                {
                                    dataToWrite += currentVar;
                                }

                                sw.Write(dataToWrite + ",");
                            }
                        }

                        foreach (FileStream fs in inputStreams)
                        {
                            fs.Close();
                        }

                        if (processedVarFilesCount == 0)
                        {
                            processedVarFilesCount += inputStreams.Count;
                        }
                        else
                        {
                            processedVarFilesCount += inputStreams.Count - 1;
                        }

                        if (Path.GetFileName(std_err_output).StartsWith("_var_aux"))
                        {
                            auxiliaryVarFilePaths.Add(std_err_output);
                        }
                    }
                }

                foreach (string filePath in auxiliaryVarFilePaths)
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Partitions the trees according to some of the criteria in their stats files and crunches their data,
        /// creating files with mean and variance computed for the new categories.
        /// </summary>
        /// <param name="bestChoiceFlagPresent"> Determines whether the provided data contain the best
        /// choice flag (which determines whether the best node was chosen in the given iteration)
        /// or not. </param>
        /// <param name="discreet"> Determines whether each value of the given criterion should be its
        /// own partition (suitable for criteria with only a few values), or whether the values should
        /// be separated into buckets according to the borders variable. </param>
        /// <param name="borders"> If <paramref name="discreet"/> is set to true, the values in this variable
        /// are used as borders between different buckets when paritioning the trees. I.e. if it contains
        /// values 5 and 10, the trees will be split into three buckets, one with trees whose values for the
        /// given variable fall below 5, one with trees between 5 (inclusive) and 10 (exclusive) and another
        /// with trees with values 10 and more. </param>
        /// <param name="critIndex"> The index of the criterion to be taken into account. </param>
        /// <param name="minFileLimit"> The minimum number of files that have to fall into a category
        /// for it to be processed. </param>
        /// <param name="inputPath"> The path to the input files. </param>
        /// <param name="outputPath"> The output path (including the name of the output file, to which a
        /// suffix will be attached - either an index, or, if <paramref name="discreet"/> is set to true
        /// the value of the variable based on which trees are aggregated). </param>
        /// <param name="statsDirPath"> The path to the directory containing the stats files. </param>
        public static void RepartitionAndCrunch(
            bool bestChoiceFlagPresent,
            bool discreet,
            double[] borders,
            int critIndex,
            int minFileLimit,
            string inputPath,
            string outputPath,
            string statsDirPath
            )
        {
            // Go through the stat files, find which trees belong in which partition, store that
            // in the partitions variable, then call CrunchItAll on every partition.

            // The criteria based on which trees can be partitioned.
            string[] criteria = {
                "Size category:",
                "Is flipped:",
                "Number of nodes:",
                "Number of terminal nodes:",
                "Number of winning end states:",
                "Number of losing end states:",
                "Ratio of winning states:",
                "Ratio of losing states:",
                "Ratio of wins to losses:",
                "Average branching factor:",
                "Accumulated depth of terminal nodes:",
                "Maximum depth:",
                "Average terminal node depth:",
                "Accumulated number of children:",
                "Maximum number of children:",
                "Is deceptive:",
                "Number of deceptive nodes:"
            };

            // Depending on whether the discreet variable is set to true, the keys are either values
            // of the given criterion or bucket indices.
            Dictionary<double, List<FileInfo>> partitioning = new Dictionary<double, List<FileInfo>>();

            DirectoryInfo inputDirInfo = new DirectoryInfo(inputPath);
            DirectoryInfo statsDirInfo = new DirectoryInfo(statsDirPath);
            List<FileInfo> inputFiles = new List<FileInfo>(inputDirInfo.GetFiles("*", SearchOption.AllDirectories));

            foreach (FileInfo inf in inputFiles)
            {
                // Skip the variance files.
                if (inf.Name.EndsWith("_var.txt"))
                {
                    continue;
                }

                // Get the name of the corresponding tree stats file.
                string sfFileName = "tree";
                bool treeEncountered = false;
                foreach (string part in inf.Name.Split('_'))
                {
                    if (part == "data" || part == "data.txt")
                    {
                        sfFileName += "_stats.txt";
                        break;
                    }

                    if (treeEncountered)
                    {
                        sfFileName += "_" + part;
                    }

                    if (part == "tree")
                    {
                        treeEncountered = true;
                    }
                }

                if (!treeEncountered)
                {
                    continue;
                }

                FileInfo sf = statsDirInfo.GetFiles(sfFileName, SearchOption.AllDirectories)[0];

                // Wins and losses need to be flipped in trees where the experiments don't start from the root node
                // (because the root node has only one child, so that child would always be picked).
                bool flipped = false;
                int currentCritIndex = critIndex;

                using (StreamReader sr = new StreamReader(sf.FullName))
                {
                    // Read the file line by line until the line containing the sought after value is found.
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Check if the wins and losses for this tree are suppossed to be flipped.
                        // If so, check if the criterion index point to one of the attributes that have
                        // to do with wins and losses and if it is, flip it.
                        if (line.StartsWith("Is flipped:"))
                        {
                            flipped = bool.Parse(line.Split(':')[1]);
                            if (flipped)
                            {
                                if (critIndex == 4)
                                {
                                    currentCritIndex = 5;
                                }
                                else if (critIndex == 5)
                                {
                                    currentCritIndex = 4;
                                }
                                else if (critIndex == 6)
                                {
                                    currentCritIndex = 7;
                                }
                                else if (critIndex == 7)
                                {
                                    currentCritIndex = 6;
                                }
                            }
                        }

                        if (line.StartsWith(criteria[currentCritIndex]))
                        {
                            // Any value, be it int, double or bool, gets converted to a double,
                            // so that it can handled in a uniform way.
                            double critVal;
                            if (!double.TryParse(line.Split(':')[1], out critVal))
                            {
                                critVal = bool.Parse(line.Split(':')[1]) ? 1 : 0;
                            }

                            // If the criterion is the ratio of wins to losses and the values that
                            // have to do with wins and losses are supposed to be flipped for this
                            // tree, compute the inverse of the value.
                            if (currentCritIndex == 8 && flipped)
                            {
                                critVal = 1 / critVal;
                            }

                            if (discreet)
                            {
                                if (!partitioning.ContainsKey(critVal))
                                {
                                    partitioning.Add(critVal, new List<FileInfo>());
                                }
                                partitioning[critVal].Add(inf);
                            }
                            else
                            {
                                // Find which bucket the tree falls into.
                                int bucketIndex;
                                for (bucketIndex = 0; bucketIndex < borders.Length; ++bucketIndex)
                                {
                                    if (critVal < borders[bucketIndex])
                                    {
                                        break;
                                    }
                                }

                                if (!partitioning.ContainsKey(bucketIndex))
                                {
                                    partitioning.Add(bucketIndex, new List<FileInfo>());
                                }

                                string fileName = "crunched_" + sf.Directory.Name + '_' + sf.Name.Replace("_stats", "_data");
                                partitioning[bucketIndex].Add(inputDirInfo.GetFiles(fileName, SearchOption.AllDirectories)[0]);
                            }

                            break;
                        }
                    }
                }
            }

            // Remove the extention from the file name so that indices can be appended to it.
            outputPath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath));

            foreach (double key in partitioning.Keys)
            {
                if (partitioning[key].Count >= minFileLimit)
                {
                    CrunchItAll(bestChoiceFlagPresent, false, outputPath + '_' + key + ".txt", partitioning[key]);
                }
            }
        }

        /// <summary>
        /// Gets the next number from the input stream.
        /// </summary>
        /// <param name="reader"> The input stream. </param>
        /// <param name="delimiter"> The caracters used to separate values in a file. </param>
        /// <exception cref="IllegalOperationException"> Thrown if the end of the file is reached before
        /// a delimiter. </exception>
        private static double GetNumericValue(FileStream reader, char delimiter)
        {
            string value = "";
            int character = reader.ReadByte();

            if (character == -1)
            {
                return double.NaN;
            }

            while (character != delimiter && character != -1)
            {
                value += (char)character;
                character = reader.ReadByte();
            }

            if (character == -1)
            {
                throw new IllegalOperationException("End of file encountered, delimiter expected.");
            }

            return double.Parse(value);
        }
    }
}
