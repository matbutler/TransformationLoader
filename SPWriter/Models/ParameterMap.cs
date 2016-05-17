using System.Data;
using System.Data.SqlClient;

namespace SPWriter.Models
{
    public class ParameterMap
    {
        public string Map { get; set; }
        public string Name { get; set; }
        public SqlDbType DbType { get; set; }
        public int? Size { get; set; }
        public object Value { get; set; }
        public bool IsGlobal { get; set; }
    }
}
