using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransformationCore.Enums;
using TransformationCore.Interfaces;

namespace PipeTest
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message, MessageLevel msgLevel)
        {
            Console.WriteLine("{0} : {1}", msgLevel, message);
        }
    }
}
