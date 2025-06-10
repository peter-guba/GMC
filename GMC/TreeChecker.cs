using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// Defines methods for checking if a tree has been generated correctly.
    /// </summary>
    internal class TreeChecker
    {
        /// <summary>
        /// Goes through the tree (the path to which is specified by the parameter) and checks whether all the chidlren are
        /// at the correct positions.
        /// </summary>
        public static void CheckTree(string treeFilePath)
        {
            Queue<long> positions = new Queue<long>();
            positions.Enqueue(0);

            using (StreamReader sr = new StreamReader(treeFilePath))
            {
                string line;
                long lastPos = 0;
                long lineCounter = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    ++lineCounter;

                    if (positions.Count != 0 && lastPos == positions.Peek())
                    {
                        if (!line.StartsWith('N') && !line.StartsWith('T'))
                        {
                            Console.WriteLine("Position " + positions.Peek() + " (line " + lineCounter + ") expected child, found \"" + line + "\"");
                        }

                        positions.Dequeue();
                    }

                    if (line.StartsWith('N'))
                    {
                        positions.Enqueue(long.Parse(line.Split('|')[1]));
                    }

                    lastPos += line.Length + 1;
                }
            }

            if (positions.Count != 0)
            {
                Console.WriteLine("Positions queue not empty => children missing.");
            }

            Console.WriteLine("Everyting ok.");
        }
    }
}
