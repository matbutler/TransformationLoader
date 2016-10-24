using System.Collections.Generic;
using System.Data;

namespace SQLBulkLoader
{
    public class MappedDataTable
    {
        public DataTable Table { get; set; }
        public Dictionary<string, string> ColumnMappings { get; set; }
    }
}
