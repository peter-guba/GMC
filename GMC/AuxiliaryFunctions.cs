using GMC.Exceptions;
using GMC.SearchAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// A class that contains various auxiliary functions used by other classes.
    /// </summary>
    internal class AuxiliaryFunctions
    {
        /// <summary>
        /// Computes the factorial of an integer larger than or equal to zero.
        /// </summary>
        /// <exception cref="IllegalOperationException"> Thrown if a number smaller than zero is passed. </exception>
        public static int Factorial(int n)
        {
            if (n < 0)
            {
                throw new IllegalOperationException("Can't compute a factorial of a number smaller than 0.");
            }

            int result = 1;

            for (int i = n; i > 0; --i)
            {
                result *= i;
            }

            return result;
        }

        /// <summary>
        /// Computes binomial n choose k.
        /// </summary>
        public static int BinomialCofficient(int n, int k)
        {
            return Factorial(n) / (Factorial(k) * Factorial(n - k));
        }

        /// <summary>
        /// Creates an instance of an algorithm based on its name.
        /// </summary>
        /// <exception cref="MissingMethodException"> Thrown if the given calls is missing the TryGetInstance method. </exception>
        /// <exception cref="ArgumentException"> Thrown if the given name is invalid. </exception>
        public static ISearch GetAlgorithm(string name)
        {
            ISearch result;

            foreach (Type t in Assembly.GetEntryAssembly().GetTypes())
            {
                if (
                    t.Namespace == "GMC.SearchAlgorithms" &&
                    t.IsAssignableTo(typeof(ISearch)) &&
                    t.IsClass)
                {
                    MethodInfo tryGetInstanceMethod = t.GetMethod("TryGetInstance");

                    if (tryGetInstanceMethod == null)
                    {
                        throw new MissingMethodException("Class " + t.Name + " is missing static TryGetInstance method.");
                    }
                    else
                    {
                        result = (ISearch)tryGetInstanceMethod.Invoke(null, new object[] { name });

                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            throw new ArgumentException(name + " doesn't specify any known algorithm.");
        }

        /// <summary>
        /// Gets a reward type based on its string specification.
        /// </summary>
        /// <param name="input"> String to be parsed. </param>
        /// <param name="startIndex"> Index at which parsing should start. </param>
        /// <param name="absorbedFields"> The number of fields (separated by '_') that have been absorbed
        /// by this method (i.e. used to specify the reward type and its parameters). </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"> Thrown when the supplied string doesn't specify a valid
        /// reward type with its parameters. </exception>
        public static RewardType GetRewardType(string input, int startIndex, out int absorbedFields)
        {
            string cutInput = input.Substring(startIndex);
            
            try
            {
                if (cutInput.StartsWith("wr"))
                {
                    absorbedFields = 1;
                    return RewardType.WinRatio;
                }

                if (cutInput.StartsWith("mm"))
                {
                    absorbedFields = 1;
                    return RewardType.Minimax;
                }

                if (cutInput.StartsWith("rnwre"))
                {
                    TreeNode.noiseMultiplier = double.Parse(cutInput.Split('_')[1]);
                    absorbedFields = 2;
                    return RewardType.RandomNoiseWREstimate;
                }

                if (cutInput.StartsWith("gcwre"))
                {
                    TreeNode.initialNoise = double.Parse(cutInput.Split('_')[1]);
                    TreeNode.maxNoiseStep = double.Parse(cutInput.Split('_')[2]);
                    absorbedFields = 3;
                    return RewardType.GradualChangeWREstimate;
                }

                throw new FormatException();
            }
            catch (FormatException e)
            {
                throw new ArgumentException("The reward type or its parameters aren't specified correctly in the string \"" + input + "\".");
            }
        }

        /// <summary>
        /// Takes the size in bytes and returns its string representation, but scaled to a more suitable unit.
        /// </summary>
        public static string GetSizeInProperUnits(double size)
        {
            if (size <= 0)
            {
                return "0B";
            }

            string[] suffixes = { "B", "kB", "MB", "GB", "TB" };

            for (int suffixIndex = 0; suffixIndex < suffixes.Length; ++suffixIndex)
            {
                if (size < 1024)
                {
                    return Math.Round(size, 3) + suffixes[suffixIndex];
                }
                else
                {
                    size /= 1024;
                }
            }

            return Math.Round(size, 3) + suffixes[suffixes.Length - 1];
        }
    }
}
