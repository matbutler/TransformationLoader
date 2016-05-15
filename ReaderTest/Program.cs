using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            var config = XElement.Parse(@"<reader name=""CSVReader"">
                                            <fields>
                                                <field name=""Transaction Date"" type=""datetime"" format=""dd/MM/yyyy""/>
                                                <field name=""Balance"" type=""decimal""/>
                                            </fields>
                                        </reader>");

            reader.Initialise(@"c:\temp\data.csv", config, null);

            reader.Load(queue, ref errorCount, ct, null, null);
        }
    }
}
