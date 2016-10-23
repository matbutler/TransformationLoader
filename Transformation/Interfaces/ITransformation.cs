using Logging;
using System.Collections.Generic;
using System.Xml.Linq;
using TransformationCore.Models;

namespace TransformationCore
{
    public interface ITransformation
    {
        void Initialise(XElement configXML, GlobalData globalData, ILogger logger, int pipeNumber);
        void Transform(Dictionary<string, object> row);
        void Close();
    }
}
