using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Helpers;
using TransformationCore.Interfaces;

namespace SQLBulkLoader
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SQLBulkLoader")]
    [ExportMetadata("Version", "1.0.0")]
    public class SQLBulkLoader : Transformation, IDisposable
    {
        private static DataTable _loggingTable;
        private static string _tablename;
        private static string _connStr;
        private static int _batchSize = 5000;
        private static int _rows = 0;
        private static int _activeInstances = 0;
        private static bool _initailised = false;
        private static object _initialisationObject = new object();
        private static Dictionary<string, string> _columns = new Dictionary<string, string>();

        protected override void Initialise(XElement configXML, ILogger logger)
        {
            if (configXML == null)
            {
                throw new ConfigException("Missing Config");
            }

            Interlocked.Increment(ref _activeInstances);

            lock (_initialisationObject)
            {
                if (_initailised)
                {
                    logger.Log("Already initailised", TransformationCore.Enums.MessageLevel.Debug);
                    return;
                }

                _tablename = configXML.Attribute("tablename")?.Value;

                if (string.IsNullOrWhiteSpace(_tablename))
                {
                    throw new ConfigException("Missing table name");
                }

                _connStr = configXML.Attribute("connection")?.Value ?? GlobalData["connection"].ToString();

                if (!string.IsNullOrWhiteSpace(configXML.Attribute("batchsize")?.Value) && !int.TryParse(configXML.Attribute("batchsize")?.Value, out _batchSize))
                {
                    throw new ConfigException("Invalid batch size");
                }

                SetupDatatable(configXML);

                _initailised = true;
            }
        }

        private static void SetupDatatable(XElement configXML)
        {
            _loggingTable = new DataTable();

            var columns = configXML.Element("columns");
            if (columns == null || columns.Elements("column").Count() == 0)
            {
                throw new ConfigException("Missing column definition");
            }

            foreach (var column in columns.Elements("column"))
            {
                var name = column.Attribute("name")?.Value;
                var type = column.Attribute("type")?.Value;
                var format = column.Attribute("format")?.Value;
                var map = string.IsNullOrWhiteSpace(column.Attribute("map")?.Value) ? name.ToLower() : column.Attribute("map").Value;

                _columns.Add(name, map);

                if (string.IsNullOrWhiteSpace(type))
                {
                    throw new ConfigException(string.Format("Invalid type for column {0}", name));
                }

                _loggingTable.Columns.Add(new DataColumn { DataType = TypeConverter.GetType(type), ColumnName = name });
            }
        }

        private void BulkCopy()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null))
                {
                    bulkCopy.DestinationTableName = _tablename;
                    bulkCopy.BatchSize = _batchSize;

                    foreach (var column in _columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.Key, column.Key);
                    }

                    bulkCopy.WriteToServer(_loggingTable);

                    _loggingTable.Clear();
                }
            }
        }

        protected override void PreTransform(Dictionary<string, object> row)
        {
            base.PreTransform(row);

            lock (_loggingTable.Rows.SyncRoot)
            {
                var rowNo = Interlocked.Increment(ref _rows);
                DataRow newRow = _loggingTable.NewRow();

                foreach (var column in _columns)
                {
                    newRow[column.Key] = row[column.Value];
                }

                _loggingTable.Rows.Add(newRow);

                if (rowNo >= _batchSize)
                {
                    BulkCopy();
                    Interlocked.Exchange(ref _rows, 0);
                }
            }
        }

        protected override void Transform()
        {
        }

        public override void Close()
        {
            var count = Interlocked.Decrement(ref _activeInstances);
            if(count == 0)
            {
                BulkCopy();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
