using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Logging;
using TransformationCore;
using TransformationCore.Interfaces;
using TransformationCore.Models;
using System.Data.SqlClient;
using TransformationCore.Exceptions;
using System.Data;
using System.ComponentModel.Composition.Hosting;

namespace SQLBulkLoader
{
    [Export(typeof(IProcessStep))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SQLBulkLoaderStep")]
    [ExportMetadata("Version", "1.0.0")]
    public class SQLBulkLoaderStep : IProcessStep
    {
        private string _connStr;
        private string _tableName;
        private string _destinationTableName;
        private int _batchSize = 5000;
        private ILogger _logger;
        private Dictionary<string, string> _columnMappings { get; set; }

        public void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger, CompositionContainer container)
        {
            if (config == null)
            {
                throw new ConfigException("Missing Config");
            }

            _tableName = config.Attribute("table")?.Value;

            if (string.IsNullOrWhiteSpace(_tableName))
            {
                throw new ConfigException("Missing table");
            }

            _destinationTableName = config.Attribute("destinationtable")?.Value;

            if (string.IsNullOrWhiteSpace(_destinationTableName))
            {
                _destinationTableName = _tableName;
            }

            if (!string.IsNullOrWhiteSpace(config.Attribute("batchsize")?.Value) && !int.TryParse(config.Attribute("batchsize")?.Value, out _batchSize))
            {
                throw new ConfigException("Invalid batch size");
            }

            _connStr = config.Attribute("connection")?.Value;

            _logger = logger;
        }

        public Task<bool> Process(XElement processInfo, GlobalData globalData, bool previousStepSucceeded = true)
        {
            if (string.IsNullOrWhiteSpace(_connStr))
            {
                _connStr = globalData.Data["connection"].ToString();
            }

            if (!globalData.CacheDataSet.Tables.Contains(_tableName))
            {
                throw new Exception(string.Format("{0} : Invalid table {1}",nameof(SQLBulkLoaderStep), _tableName));
            }

            if (_columnMappings == null)
            {
                _columnMappings = globalData.CacheDataSet.Tables[_tableName].Columns.Cast<DataColumn>().ToDictionary(x => x.ColumnName, x => x.ColumnName);
            }

            try
            {
                BulkCopy(globalData);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error("Error writing batch data", ex);
            }

            return Task.FromResult(false);
        }

        private void BulkCopy(GlobalData globalData)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null))
                {
                    bulkCopy.DestinationTableName = _destinationTableName;
                    bulkCopy.BatchSize = _batchSize;

                    foreach (var column in _columnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(column.Key, column.Value);
                    }

                    bulkCopy.WriteToServer(globalData.CacheDataSet.Tables[_tableName]);
                }
            }
        }
    }
}
