using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Enums;
using TransformationCore.Exceptions;
using TransformationCore.Helpers;

namespace TransformationCore
{
    public class TransformationFilter
    {
        private List<object> _values;
        private TransformationFilterOperatorEnum _operator;

        public string FieldName { get; set; }

        public TransformationFilter(XElement filterConfig)
        {
            if (filterConfig.Attribute("field") == null || filterConfig.Attribute("operator") == null || filterConfig.Attribute("value") == null)
            {
                throw new TransformationFilterException(string.Format("Invalid Filter {0}", filterConfig.Attribute("field")?.Value));
            }

            FieldName = filterConfig.Attribute("field").Value.ToLower();

            _operator = TransformationFilterOperatorEnum.Equal;
            if (!Enum.TryParse(filterConfig.Attribute("operator").Value, true, out _operator))
            {
                throw new TransformationFilterException(string.Format("Invalid operator {1} for Filter {0}", filterConfig.Attribute("field").Value, filterConfig.Attribute("operator").Value));
            }

            UpdateFilterValues(filterConfig);
        }

        private void UpdateFilterValues(XElement filterConfig)
        {
            _values = new List<object>();

            var values = filterConfig.Attribute("value").Value;
            if (values.IndexOf(",") > 0)
            {
                var filtervals = values.Split(',').ToList();

                foreach (var filterVal in filtervals)
                {
                    _values.Add(TypeConverter.GetConverter(filterConfig.Attribute("filtertype")?.Value, filterConfig.Attribute("format")?.Value)(filterVal));
                }
            }
            else
            {
                _values.Add(TypeConverter.GetConverter(filterConfig.Attribute("filtertype")?.Value, filterConfig.Attribute("format")?.Value)(values));
            }
        }

        public bool Check(object rowVal)
        {
            return !_values.Any(val =>
            {
                if (val == null)
                {
                    if (rowVal is DateTime && (DateTime)rowVal == DateTime.MinValue)
                    {
                        rowVal = null;
                    }
                    else if (rowVal is string && string.IsNullOrEmpty((string)rowVal))
                    {
                        rowVal = null;
                    }

                    if ((_operator == TransformationFilterOperatorEnum.Equal && rowVal == null) || (_operator == TransformationFilterOperatorEnum.NotEqual && rowVal != null))
                    {
                        return true;
                    }
                }
                else
                {
                    if (rowVal is string)
                    {
                        rowVal = rowVal.ToString().ToLower();
                        val = val.ToString().ToLower();
                    }

                    var compResult = ((IComparable)rowVal).CompareTo(val);
                    if ((_operator == TransformationFilterOperatorEnum.LessThan && compResult == -1) || (_operator == TransformationFilterOperatorEnum.LessThanEqual && compResult <= 0) || (_operator == TransformationFilterOperatorEnum.Equal && compResult == 0) || (_operator == TransformationFilterOperatorEnum.GreaterThanEqual && compResult >= 0) || (_operator == TransformationFilterOperatorEnum.GreaterThan && compResult == 1) || (_operator == TransformationFilterOperatorEnum.NotEqual && compResult != 0))
                    {
                        return true;
                    }
                }

                return false;
            });
        }
    }
}
