using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Exceptions
{
    public class TransformationFilterException : Exception
    {
        public TransformationFilterException(string message) : base(message)
        {

        }
    }
}
