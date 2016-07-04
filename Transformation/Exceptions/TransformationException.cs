using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Exceptions
{
    public class TransformationException : Exception
    {
        public TransformationException(string message) : base(message)
        {

        }
    }
}
