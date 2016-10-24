using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Helpers;

namespace SQLBulkLoader
{
    public static class DataTableBuilder
    {
        public static MappedDataTable Build(XElement configXML)
        {
            var tablename = configXML.Attribute("tablename")?.Value;

            if (string.IsNullOrWhiteSpace(tablename))
            {
                throw new Exception("Missing table name");
            }

            var loggingTable = new MappedDataTable()
            {
                Table = new DataTable(tablename),
                ColumnMappings = new Dictionary<string, string>(),
            };

            var columns = configXML.Element("columns");
            if (columns == null || columns.Elements("column").Count() == 0)
            {
                throw new Exception("Missing column definition");
            }

            foreach (var column in columns.Elements("column"))
            {
                var name = column.Attribute("name")?.Value;
                var type = column.Attribute("type")?.Value;
                var format = column.Attribute("format")?.Value;
                var map = string.IsNullOrWhiteSpace(column.Attribute("map")?.Value) ? name.ToLower() : column.Attribute("map").Value;

                loggingTable.ColumnMappings.Add(name, map);

                if (string.IsNullOrWhiteSpace(type))
                {
                    throw new Exception(string.Format("Invalid type for column {0}", name));
                }

                loggingTable.Table.Columns.Add(new DataColumn { DataType = TypeConverter.GetType(type), ColumnName = name });
            }

            return loggingTable;
        }
    }
}
