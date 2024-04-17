using System.Linq.Expressions;
using static ExpRT.Core.ExpressionBuilderHelper;

namespace ExpRT
{
    public static class ExpressionBuilder
    {

        public static Expression<Func<TSource, bool>> BuildFilterExpression<TSource>(string query)
        {
            return GetFilterExpression<TSource>(query);
        }

        public static Expression<Func<TSource, bool>> BuildFilterExpression<TSource>(List<string> nodes)
        {
            return GetFilterExpression<TSource>(nodes);
        }
    }
}
