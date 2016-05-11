using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransformationCore.Enums;

namespace TransformationCore.Interfaces
{
    public interface ILogger
    {
        void Log(string message, MessageLevel msgLevel);
    }
}
