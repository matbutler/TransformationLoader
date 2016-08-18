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

        protected override void Initialise(XElement configXML, ILogger logger)
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

            _connStr = configXML.Attribute("connection")?.Value ?? GlobalData["connection"].ToString();

            SetupParameters(configXML);
        }

        private void SetupParameters(XElement configXML)
        {
            var columns = configXML.Element("columns");
            if (columns == null || columns.Elements("column").Count() == 0)
            {
                throw new ConfigException("Missing column definition");
            }

            _parameters = new List<ParameterMap>();

            foreach (var column in columns.Elements("column"))
            {
                var name = column.Attribute("name")?.Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ConfigException("Missing column name");
                }

                var size = column.Attribute("size")?.Value ?? "";
                var map = string.IsNullOrWhiteSpace(column.Attribute("map")?.Value) ? name.ToLower() : column.Attribute("map").Value;

                int sizeVal = 0;

                _parameters.Add(new ParameterMap
                {
                    Map = map,
                    Name = "@" + name,
                    DbType = TypeConverter.GetDBType(column.Attribute("type")?.Value ?? ""),
                    Size = int.TryParse(size, out sizeVal) ? (int?)sizeVal : null,
                    Value = map.StartsWith("@") ? GlobalData[map] : null,
                    IsGlobal = map.StartsWith("@"),
                });
            }
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
