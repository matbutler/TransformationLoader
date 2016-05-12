using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private string fileName;

        public void initialise(string fileName, XElement config, ILogger logger)
        {
            this.fileName = fileName;
        }

        public void load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, CancellationToken ct, ILogger logger, Action<bool, bool, int, string> rowLogAction)
        {
            var sw = new Stopwatch();

            sw.Start();
            Console.WriteLine("Started");
            using (CsvReader csv = new CsvReader(new StreamReader(this.fileName), true))
            {
                csv.Columns = new List<LumenWorks.Framework.IO.Csv.Column>
                {
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Transaction Date", Type = typeof(DateTime) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Transaction Type", Type = typeof(string) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Sort Code", Type = typeof(string) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Account Number", Type = typeof(string) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Transaction Description", Type = typeof(string) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Debit Amount", Type = typeof(decimal) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Credit Amount", Type = typeof(decimal) },
                    new LumenWorks.Framework.IO.Csv.Column { Name = "Balance", Type = typeof(decimal) },
                };

                int fieldCount = csv.FieldCount;
                int count = 0;
                string[] headers = csv.GetFieldHeaders();
                while (csv.ReadNextRecord())
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < fieldCount; i++)
                    {
                        row.Add(headers[i], csv[i]);
                    }

                    inputQueue.Add(row);
                    count++;
                }

                sw.Stop();
                Console.WriteLine("Stop {1} rows in {0} ({2} per sec)", sw.Elapsed.TotalSeconds, count, sw.Elapsed.TotalSeconds / count);
                Console.ReadKey();
            }

        }
    }
}
