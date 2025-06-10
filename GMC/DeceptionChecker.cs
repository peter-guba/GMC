using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Defines methods for checking if a tree is deceptive. This means that it has at least one node
    /// which isn't minimax-optimal, but its win ratio value is higher than that of any minimax-optimal node.
    /// </summary>
    internal class DeceptionChecker
    {
        /// <summary>
        /// Checks if a tree specified in a given file is deceptive.
        /// </summary>
        public static bool IsDeceptive(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                bool moreThanOneChild = false;
                int counter = 0;
                List<string> rootChildren = new List<string>();
                bool povPlayer = false;

                while (true)
                {
                    string? line = sr.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith('#') || line.StartsWith('/'))
                    {
                        if (moreThanOneChild)
                        {
                            break;
                        }
                        else
                        {
                            counter = 0;
                            rootChildren.Clear();
                        }

                        if (line.StartsWith('#'))
                        {
                            povPlayer = !povPlayer;
                        }
                    }
                    else
                    {
                        ++counter;
                        rootChildren.Add(line);

                        if (counter > 1)
                        {
                            moreThanOneChild = true;
                        }
                    }
                }

                double bestMMVal = -1;
                double bestWRVal = -1;
                List<string> bestWRChildren = new List<string>();

                foreach (string child in rootChildren)
                {
                    double wrVal = double.Parse(child.Split(',')[1]);
                    double mmVal = double.Parse(child.Split(',')[2]);

                    if (!povPlayer)
                    {
                        wrVal = 1 - wrVal;
                        mmVal = 1 - mmVal;
                    }

                    if (mmVal > bestMMVal)
                    {
                        bestMMVal = mmVal;
                    }

                    if (wrVal == bestWRVal)
                    {
                        bestWRChildren.Add(child);
                    }
                    else if (wrVal > bestWRVal)
                    {
                        bestWRVal = wrVal;
                        bestWRChildren = new List<string>() { child };
                    }
                }

                bool deceptive = false;

                foreach (string bwrch in bestWRChildren)
                {
                    double mmVal = double.Parse(bwrch.Split(',')[2]);
                    mmVal = povPlayer ? mmVal : 1 - mmVal;
                    if (mmVal != bestMMVal)
                    {
                        deceptive = true;
                        break;
                    }
                }

                return deceptive;
            }
        }
    }
}
