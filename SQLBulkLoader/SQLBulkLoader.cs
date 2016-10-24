using Logging;
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
using TransformationCore.Models;

namespace SQLBulkLoader
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SQLBulkLoader")]
    [ExportMetadata("Version", "1.0.0")]
    public class SQLBulkLoader : Transformation, IDisposable
    {
        private static MappedDataTable _loggingTable;
        private static string _tablename;
        private static string _connStr;
        private static int _batchSize = 5000;
        private static int _rows = 0;
        private static int _activeInstances = 0;
        private static bool _initailised = false;
        private static object _initialisationObject = new object();

        protected override void Initialise(XElement configXML, GlobalData globalData, ILogger logger)
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
                    logger.Debug(string.Format("Pipe {0}: {1} Already initailised", PipeNumber, nameof(SQLBulkLoader)));
                    return;
                }

                _tablename = configXML.Attribute("tablename")?.Value;

                if (string.IsNullOrWhiteSpace(_tablename))
                {
                    throw new ConfigException("Missing table name");
                }

                _connStr = configXML.Attribute("connection")?.Value ?? GlobalData.Data["connection"].ToString();

                if (!string.IsNullOrWhiteSpace(configXML.Attribute("batchsize")?.Value) && !int.TryParse(configXML.Attribute("batchsize")?.Value, out _batchSize))
                {
                    throw new ConfigException("Invalid batch size");
                }

                try
                {
                    _loggingTable = DataTableBuilder.Build(configXML);
                }
                catch (Exception ex)
                {
                    throw new ConfigException("Invalid Configuration: " + ex.Message);
                }

                _initailised = true;
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

                    foreach (var column in _loggingTable.ColumnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(column.Key, column.Key);
                    }

                    bulkCopy.WriteToServer(_loggingTable.Table);

                    _loggingTable.Table.Clear();
                }
            }
        }

        protected override void PreTransform(Dictionary<string, object> row)
        {
            base.PreTransform(row);

            lock (_loggingTable.Table.Rows.SyncRoot)
            {
                var rowNo = Interlocked.Increment(ref _rows);
                DataRow newRow = _loggingTable.Table.NewRow();

                foreach (var column in _loggingTable.ColumnMappings)
                {
                    newRow[column.Key] = row[column.Value];
                }

                _loggingTable.Table.Rows.Add(newRow);

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
