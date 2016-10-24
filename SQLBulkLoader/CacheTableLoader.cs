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
    [ExportMetadata("Name", "CacheTableLoader")]
    [ExportMetadata("Version", "1.0.0")]
    public class CacheTableLoader : Transformation
    {
        private static MappedDataTable _loggingTable;
        private static bool _initailised = false;
        private static object _initialisationObject = new object();

        protected override void Initialise(XElement configXML, GlobalData globalData, ILogger logger)
        {
            if (configXML == null)
            {
                throw new ConfigException("Missing Config");
            }

            lock (_initialisationObject)
            {
                if (_initailised)
                {
                    logger.Debug(string.Format("Pipe {0}: {1} Already initailised", PipeNumber, nameof(CacheTableLoader)));
                    return;
                }

                try
                {
                    _loggingTable = DataTableBuilder.Build(configXML);
                }
                catch(Exception ex)
                {
                    throw new ConfigException("Invalid Configuration: " + ex.Message);
                }

                globalData.CacheDataSet.Tables.Add(_loggingTable.Table);

                _initailised = true;
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
