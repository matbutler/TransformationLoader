using log4net;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TransformationCore
{
    interface ITransformation
    {
        bool Initialise(XElement configXML, Dictionary<string, object> globalData, ILog logger);

        Dictionary<string, object> Transform(Dictionary<string, object> row);
    }
}
