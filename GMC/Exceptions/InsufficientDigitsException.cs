using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.Exceptions
{
    /// <summary>
    /// Signifies that there is an insufficient number of digits to express the positions of children in a tree file.
    /// </summary>
    internal class InsufficientDigitsException : Exception
    {
        public InsufficientDigitsException() { }

        public InsufficientDigitsException(string message) : base(message) { }
    }
}
