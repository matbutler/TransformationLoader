using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using System.Linq.Expressions;
using System.Reflection;

namespace TransformationCore
{
    public abstract class Transformation : ITransformation
    {
        private Dictionary<string, object> _filters;
        public bool Initialise(XElement configXML, Dictionary<string, object> globalData, ILog logger)
        {
            if (configXML == null)
            {
                return true;
            }

            _filters = new Dictionary<string, object>();
            if (configXML.Element("config") != null && configXML.Element("config").Elements("filter") != null)
            {
                foreach (var filterConfig in configXML.Elements("config").Elements("filter"))
                {
                    var filter = new TransformationFilter();

                    filter.Initialise(filterConfig);

                    _filters.Add("", filter);
                }
            }

            return true;
        }

        public Dictionary<string, object> Transform(Dictionary<string, object> row)
        {
            throw new NotImplementedException();
        }

        static Func<object, object> BuildGetAccessor(MethodInfo method)
        {
            var obj = Expression.Parameter(typeof(object), "o");

            Expression<Func<object, object>> expr =
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(obj, method.DeclaringType),
                            method),
                        typeof(object)),
                    obj);

            return expr.Compile();
        }

        static Action<object, object> BuildSetAccessor(MethodInfo method)
        {
            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));

            Expression<Action<object, object>> expr =
                Expression.Lambda<Action<object, object>>(
                    Expression.Call(
                        Expression.Convert(obj, method.DeclaringType),
                        method,
                        Expression.Convert(value, method.GetParameters()[0].ParameterType)),
                    obj,
                    value);

            return expr.Compile();
        }
    }
}
