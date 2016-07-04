using System.Collections.Generic;
using System.Xml.Linq;
using TransformationCore.Interfaces;

namespace TransformationCore
{
    public interface ITransformation
    {
        void Initialise(XElement configXML, Dictionary<string, object> globalData, ILogger logger);
        void Transform(Dictionary<string, object> row);
        void Close();
    }
}
