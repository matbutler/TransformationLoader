﻿using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Helpers;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace CSVReader
{
    [Export(typeof(IReader))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "CSVReader")]
    [ExportMetadata("Version", "1.0.0")]
    public class CSVReader : IReader
    {
        private string _fileName;
        private char _delimeter = ',';
        private bool _hasHeader = true;
        private List<ReaderField> _fields = new List<ReaderField>();

        public void Initialise(string fileName, XElement config, ILogger logger)
        {
            _fileName = fileName;

            if (config == null)
            {
                throw new ConfigException("Invalid CSV config");
            }

            if (config.Attribute("delimeter") != null && !string.IsNullOrWhiteSpace(config.Attribute("delimeter").Value))
            {
                _delimeter = config.Attribute("delimeter").Value.ToCharArray()[0];
            }

            if (config.Attribute("hasheader") != null && !string.IsNullOrWhiteSpace(config.Attribute("hasheader").Value))
            {
                _hasHeader = config.Attribute("hasheader").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (config.Element("fields") == null || config.Element("fields").Elements("field").Count() == 0)
            {
                throw new ConfigException("No fields have been defined");
            }

            _fields = SetupReaderFields(config);
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
                var map = configField.Attribute("map")?.Value ?? name.ToLower();
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
                    Map = map,
                    Index = index,
                    Converter = TypeConverter.GetConverter(type, configField.Attribute("format")?.Value),
                });
            }

            return readerFields;
        }


        public void Load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, CancellationToken ct, ILogger logger, Action<bool, bool, int, string> rowLogAction)
        {
            using (CsvReader csv = new CsvReader(new StreamReader(_fileName), _hasHeader, _delimeter))
            {
                LookupFieldIndexFromName(csv);

                while (csv.ReadNextRecord())
                {
                    var row = new Dictionary<string, object>();

                    foreach (var field in _fields)
                    {
                        row.Add(field.Map, field.Converter(csv[field.Index.Value]));
                    }

                    row.Add("#row", csv.CurrentRecordIndex);

                    inputQueue.Add(row);
                }
            }
        }

        private void LookupFieldIndexFromName(CsvReader csv)
        {
            if (_hasHeader)
            {
                var headers = csv.GetFieldHeaders().Select((x, i) => new { Name = x.ToLower(), Index = i }).ToDictionary(x => x.Name, x => x.Index);

                foreach (var field in _fields)
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
