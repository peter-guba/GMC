using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.Exceptions
{
    /// <summary>
    /// Signifies that an attempt was made to call some method in an inappropriate way,
    /// like trying to get the children of a terminal node for example.
    /// </summary>
    internal class IllegalOperationException : Exception
    {
        public IllegalOperationException() { }

        public IllegalOperationException(string message) : base(message) { }
    }
}
