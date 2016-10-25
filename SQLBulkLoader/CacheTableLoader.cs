using Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Models;

namespace SQLBulkLoader
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "CacheTableLoader")]
    [ExportMetadata("Version", "1.0.0")]
    public class CacheTableLoader : Transformation
    {
        private static MappedDataTable _loggingTable;
        private static object _initialisationObject = new object();

        protected override void Initialise(XElement configXML, GlobalData globalData, ILogger logger)
        {
            if (configXML == null)
            {
                throw new ConfigException("Missing Config");
            }

            var tablename = configXML.Attribute("tablename")?.Value;
            if (tablename == null)
            {
                throw new ConfigException("Missing Table name");
            }

            lock (_initialisationObject)
            {
                if (!globalData.CacheDataSet.Tables.Contains(tablename))
                {
                    logger.Debug(string.Format("Pipe {0}: {1} Already initailised", PipeNumber, nameof(CacheTableLoader)));
                    return;
                }

                try
                {
                    _loggingTable = DataTableBuilder.Build(configXML);
                }
                catch (Exception ex)
                {
                    throw new ConfigException("Invalid Configuration: " + ex.Message);
                }

                globalData.CacheDataSet.Tables.Add(_loggingTable.Table);
            }
        }


        protected override void PreTransform(Dictionary<string, object> row)
        {
            base.PreTransform(row);

            lock (_loggingTable.Table.Rows.SyncRoot)
            {
                DataRow newRow = _loggingTable.Table.NewRow();

                foreach (var column in _loggingTable.ColumnMappings)
                {
                    newRow[column.Key] = row[column.Value];
                }

                _loggingTable.Table.Rows.Add(newRow);
            }
        }

        protected override void Transform()
        {
        }
    }
}
