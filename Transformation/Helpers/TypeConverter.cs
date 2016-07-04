using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Helpers
{
    public static class TypeConverter
    {
        public static Func<string, object> GetConverter(string type, string format)
        {
            switch (type.ToUpper())
            {
                case "DATETIME":
                    if (string.IsNullOrWhiteSpace(format))
                    {
                        throw new FormatException(string.Format("Invalid format {0}", type));
                    }

                    return x => DateTime.ParseExact(x, format, null);
                case "INT":
                    return x => (object)long.Parse(x);
                case "LONG":
                    return x => (object)long.Parse(x);
                case "DECIMAL":
                    return x => (object)decimal.Parse(x);
                case "STRING":
                    return x => x;
                case "BOOL":
                    return x => (object)Convert.ToBoolean(x);
                default:
                    throw new InvalidCastException(string.Format("Invalid type {0}", type));
            }
        }

        public static Type GetType(string type)
        {
            switch (type.ToUpper())
            {
                case "DATETIME":
                    return typeof(DateTime);
                case "INT":
                    return typeof(int);
                case "LONG":
                    return typeof(long);
                case "DECIMAL":
                    return typeof(decimal);
                case "STRING":
                    return typeof(string);
                case "BOOL":
                    return typeof(bool);
                default:
                    throw new InvalidCastException(string.Format("Invalid type {0}", type));
            }
        }

        public static SqlDbType GetDBType(string type)
        {
            switch (type.ToUpper())
            {
                case "DATETIME":
                    return SqlDbType.DateTime;
                case "INT":
                    return SqlDbType.Int;
                case "LONG":
                    return SqlDbType.BigInt;
                case "DECIMAL":
                    return SqlDbType.Decimal;
                case "STRING":
                    return SqlDbType.NVarChar;
                case "BOOL":
                    return SqlDbType.Bit;
                default:
                    throw new InvalidCastException(string.Format("Invalid type {0}", type));
            }
        }
    }
}
