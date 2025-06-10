using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.Exceptions
{
    /// <summary>
    /// Signifies that a part of the code has been reached which should be unreachable.
    /// </summary>
    internal class ImpossibleException : Exception
    {
        public ImpossibleException() { }

        public ImpossibleException(string message) : base(message) { }
    }
}
