using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Helpers
{
    public static class TypeConverter
    {
        public static Func<string, object> GetConverter(string type, string format)
        {
            switch (type)
            {
                case "DATETIME":
                    if (string.IsNullOrWhiteSpace(format))
                    {
                        throw new FormatException(string.Format("Invalid format {0}", type));
                    }

                    return x => DateTime.ParseExact(x, format, null);
                case "NUMBER":
                    return x => (object)long.Parse(x);
                case "DECIMAL":
                    return x => (object)decimal.Parse(x);
                case "TEXT":
                    return x => x;
                case "BOOL":
                    return x => (object)Convert.ToBoolean(x);
                default:
                    throw new InvalidCastException(string.Format("Invalid type {0}", type));
            }
        }
    }
}
