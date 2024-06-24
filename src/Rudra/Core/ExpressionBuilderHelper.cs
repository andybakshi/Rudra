using System.Linq.Expressions;
using static Rudra.Core.ExpressionBuilderCore;

namespace Rudra.Core
{
    internal static class ExpressionBuilderHelper
    {
        internal static Expression<Func<TSource, bool>> GetFilterExpression<TSource>(object value)
        {
            switch (value)
            {
                case string query:
                    {
                        ParameterExpression parameterExp = Expression.Parameter(typeof(TSource), "param");

                        Expression expression = BuildExpression(query, parameterExp);

                        return Expression.Lambda<Func<TSource, bool>>(expression, parameterExp);
                    }

                case List<string> nodes:
                    {
                        ParameterExpression parameterExp = Expression.Parameter(typeof(TSource), "param");

                        Expression expression = BuildExpression(nodes, parameterExp);

                        return Expression.Lambda<Func<TSource, bool>>(expression, parameterExp);
                    }

                default:
                    throw new NotSupportedException("Query Input Type not supported");
            }
        }
    }
}
