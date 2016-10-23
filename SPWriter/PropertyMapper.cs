using SPWriter.Models;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TransformationCore.Exceptions;
using TransformationCore.Helpers;

namespace SPWriter
{
    public static class PropertyMapper
    {
        public static List<ParameterMap> Map(XElement configXML)
        {
            var columns = configXML.Element("columns");
            if (columns == null || columns.Elements("column").Count() == 0)
            {
                throw new ConfigException("Missing column definition");
            }

            var parameters = new List<ParameterMap>();

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

                parameters.Add(new ParameterMap
                {
                    Map = map,
                    Name = "@" + name,
                    DbType = TypeConverter.GetDBType(column.Attribute("type")?.Value ?? ""),
                    Size = int.TryParse(size, out sizeVal) ? (int?)sizeVal : null,
                    IsGlobal = map.StartsWith("@"),
                });
            }

            return parameters;
        }
    }
}
