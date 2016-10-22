using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;

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

            var config = XElement.Parse(@"<reader name=""CSVReader"" delimeter=""|"">
                                                <fields>
                                                    <field name=""personnel Number"" type=""int""/>
                                                    <field name=""position"" type=""string""/>
                                                </fields>
                                            </reader>");

            var processInfo = new XElement("processInfo", new XElement("filename", @"c:\temp\stafftest.csv"));

            reader.Initialise(processInfo, config, 1,  null);

            var sw = new Stopwatch();
            sw.Start();
            reader.Load(queue, ref errorCount, ct, null, null);
            sw.Stop();
            Console.WriteLine("Stop {1} rows in {0} ({2:#,###} per sec)", sw.Elapsed.TotalSeconds, queue.Count, (decimal)(queue.Count / sw.Elapsed.TotalSeconds));
            Console.ReadKey();
        }
    }
}
