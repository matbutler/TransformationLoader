using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Interfaces;

namespace CSVReader
{
    public class CSVReader : IReader
    {
        public void initialise(XElement config, ILogger logger)
        {
            throw new NotImplementedException();
        }

        public void load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, CancellationToken ct, ILogger logger, Action<bool, bool, int, string> rowLogAction)
        {
            throw new NotImplementedException();
        }
    }
}
