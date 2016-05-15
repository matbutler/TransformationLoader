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
using TransformationCore.Exceptions;
using TransformationCore.Helpers;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace CSVReader
{
    public class CSVReader : IReader
    {
        private string fileName;
        private char delimeter = ',';
        private bool hasHeader = true;
        private List<ReaderField> fields = new List<ReaderField>();

        public void Initialise(string fileName, XElement config, ILogger logger)
        {
            this.fileName = fileName;

            if (config == null)
            {
                throw new ConfigException("Invalid CSV config");
            }

            if (config.Attribute("delimeter") != null && !string.IsNullOrWhiteSpace(config.Attribute("delimeter").Value))
            {
                this.delimeter = config.Attribute("delimeter").Value.ToCharArray()[0];
            }

            if (config.Attribute("hasheader") != null && !string.IsNullOrWhiteSpace(config.Attribute("hasheader").Value))
            {
                this.hasHeader = config.Attribute("hasheader").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (config.Element("fields") == null || config.Element("fields").Elements("field").Count() == 0)
            {
                throw new ConfigException("No fields have been defined");
            }

            this.fields = SetupReaderFields(config);
        }

        private List<ReaderField> SetupReaderFields(XElement config)
        {
            var readerFields = new List<ReaderField>();

            var configFields = config.Element("fields").Elements("field").ToList();
            foreach (var configField in configFields)
            {
                if (configField.Attribute("name") == null || string.IsNullOrWhiteSpace(configField.Attribute("name").Value))
                {
                    throw new ConfigException("Missing field name");
                }

                if (configField.Attribute("type") == null || string.IsNullOrWhiteSpace(configField.Attribute("type").Value))
                {
                    throw new ConfigException("Missing field type");
                }

                var name = configField.Attribute("name").Value;
                var type = configField.Attribute("type").Value.ToUpper();
                int? index = null;

                if (configField.Attribute("index") != null && !string.IsNullOrWhiteSpace(configField.Attribute("index").Value))
                {
                    var indexValue = 0;
                    if (!int.TryParse(configField.Attribute("index").Value, out indexValue))
                    {
                        throw new ConfigException(string.Format("Invalid index for field : {0}", name));
                    }

                    index = indexValue;
                }

                readerFields.Add(new ReaderField
                {
                    Name = name,
                    Index = index,
                    Converter = TypeConverter.GetConverter(type, configField.Attribute("format")?.Value),
                });
            }

            return readerFields;
        }


        public void Load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, CancellationToken ct, ILogger logger, Action<bool, bool, int, string> rowLogAction)
        {
            using (CsvReader csv = new CsvReader(new StreamReader(this.fileName), this.hasHeader, this.delimeter))
            {
                int fieldCount = csv.FieldCount;

                var sw = new Stopwatch();

                sw.Start();

                LookupFieldIndexFromName(csv);

                int count = 0;
                while (csv.ReadNextRecord())
                {
                    var row = new Dictionary<string, object>();

                    foreach (var field in this.fields)
                    {
                        row.Add(field.Name, field.Converter(csv[field.Index.Value]));
                    }

                    row.Add("#row", count);

                    inputQueue.Add(row);
                    count++;
                }

                sw.Stop();
                Console.WriteLine("Stop {1} rows in {0} ({2:#,###} per sec)", sw.Elapsed.TotalSeconds, count, (decimal)(count / sw.Elapsed.TotalSeconds));
                Console.ReadKey();
            }

        }

        private void LookupFieldIndexFromName(CsvReader csv)
        {
            if (this.hasHeader)
            {
                var headers = csv.GetFieldHeaders().Select((x, i) => new { Name = x, Index = i }).ToDictionary(x => x.Name, x => x.Index);

                foreach (var field in this.fields)
                {
                    if (!field.Index.HasValue)
                    {
                        if (!headers.ContainsKey(field.Name))
                        {
                            throw new ReaderException(string.Format("Missing Field {0}", field.Name));
                        }

                        field.Index = headers[field.Name];
                    }
                }
            }
        }
    }
}
