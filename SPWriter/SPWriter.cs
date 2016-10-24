using Logging;
using SPWriter.Models;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Helpers;
using TransformationCore.Models;

namespace SPWriter
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SPWriter")]
    [ExportMetadata("Version", "1.0.0")]
    public class SPWriter : Transformation
    {
        private string _storedProcedure;
        private string _connStr;
        private List<ParameterMap> _parameters;

        protected override void Initialise(XElement configXML, GlobalData globalData, ILogger logger)
        {
            if (configXML == null)
            {
                throw new ConfigException("Missing Config");
            }

            _storedProcedure = configXML.Attribute("procedure")?.Value;

            if (string.IsNullOrWhiteSpace(_storedProcedure))
            {
                throw new ConfigException("Missing stored procedure name");
            }

            _connStr = configXML.Attribute("connection")?.Value ?? GlobalData.Data["connection"].ToString();

            _parameters = PropertyMapper.Map(configXML);
            SetGlobalParameters();
        }

        private void SetGlobalParameters()
        {
            var globalParameters = _parameters.Where(x => x.IsGlobal).ToList();

            globalParameters.ForEach(x => x.Value = GlobalData.Data[x.Map]);
        }

        protected override void PreTransform(Dictionary<string, object> row)
        {
            base.PreTransform(row);

            foreach (var parameter in _parameters)
            {
                if (!parameter.IsGlobal)
                {
                    parameter.Value = row[parameter.Map];
                }
            }
        }

        protected override void Transform()
        {
            using (var conn = new SqlConnection(_connStr))
            using (var command = new SqlCommand(_storedProcedure, conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                foreach(var parameter in _parameters)
                {
                    if (parameter.Size.HasValue)
                    {
                        command.Parameters.Add(parameter.Name, parameter.DbType, parameter.Size.Value).Value = parameter.Value;
                    }
                    else
                    {
                        command.Parameters.Add(parameter.Name, parameter.DbType).Value = parameter.Value;
                    }
                }
                
                conn.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
