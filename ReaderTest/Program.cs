using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new CSVReader.CSVReader();

            var queue = new BlockingCollection<Dictionary<string, object>>();
            int errorCount = 0;
            var ct = new CancellationTokenSource().Token;

            reader.initialise(@"c:\temp\data.csv", null, null);

            reader.load(queue, ref errorCount, ct, null, null);
        }
    }
}
