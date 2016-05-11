using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Helpers
{
    public static class LinqAccessors
    {
        public static Func<object, object> BuildGetAccessor(MethodInfo method)
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

        public static Action<object, object> BuildSetAccessor(MethodInfo method)
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
