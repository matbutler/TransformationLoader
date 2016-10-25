using Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Attributes;
using TransformationCore.Enums;
using TransformationCore.Models;

namespace SPWriter
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SQLLookup")]
    [ExportMetadata("Version", "1.0.0")]
    public class SQLLookup : Transformation, IDisposable
    {
        private static Dictionary<string, object> _lookupTable;
        private static object _lock = new object();
        private static bool _initailised = false;
        private static int _activeInstances = 0;

        [TransformationFieldAttrib(TransformationFieldTypeEnum.Config, true)]
        public string StoredProcedure { get; set; }

        [TransformationFieldAttrib(TransformationFieldTypeEnum.Config, true)]
        public string Lookup { get; set; }

        [TransformationFieldAttrib(TransformationFieldTypeEnum.Output, true)]
        public object Result { get; set; }

        [TransformationFieldAttrib(TransformationFieldTypeEnum.Config, false)]
        public bool ErrorOnNotFound { get; set; } = false;

        protected override void Initialise(XElement configXML, GlobalData globalData, ILogger logger)
        {

            var connStr = configXML.Attribute("connection")?.Value ?? GlobalData.Data["connection"].ToString();

            Interlocked.Increment(ref _activeInstances);

            lock (_lock)
            {
                if (_initailised)
                {
                    logger.Debug(string.Format("Pipe {0}: {1} Already initailised", PipeNumber, nameof(SQLLookup)));
                    return;
                }

                _lookupTable = SetupLookupTable(connStr);
            }
        }

        private Dictionary<string, object> SetupLookupTable(string connStr)
        {
            var lookupTable = new Dictionary<string, object>();
            using (var conn = new SqlConnection(connStr))
            using (var command = new SqlCommand(StoredProcedure, conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                conn.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lookupTable.Add(reader["Lookup"].ToString(), reader["Value"]);
                    }
                }
            }

            return lookupTable;
        }

        protected override void Transform()
        {
            object result = null;

            if(_lookupTable.TryGetValue(Lookup, out result))
            {
                Result = result;
                return;
            }

            if (ErrorOnNotFound)
            {
                throw new Exception(string.Format("{0} unable to find {2} (Row {1})", nameof(SQLLookup), RowNo, Lookup));
            }
        }

        public override void Close()
        {
            var count = Interlocked.Decrement(ref _activeInstances);
            if (count == 0)
            {
                lock (_lock)
                {
                    _initailised = false;
                    _lookupTable = null;
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
