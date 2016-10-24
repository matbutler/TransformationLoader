using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Exceptions
{
    public class FieldConversionException : Exception
    {
        public FieldConversionException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
