using System.Linq.Expressions;

namespace MiCake.Cord.LinqFilter.Extensions
{
    public static class SortingExtensions
    {
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, List<Sort> orders)
        {
            foreach (var order in orders)
            {
                query = query.Sort(order);
            }
            return query;
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> query, Sort order)
        {
            if (order == null || order.PropertyName == null)
            {
                return query;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            Expression property = ExpressionHelpers.BuildNestedPropertyExpression(pe, order.PropertyName);

            var sortExpression = Expression.Lambda<Func<T, object>>
                (Expression.Convert(property, typeof(object)), pe);

            if (order.Ascending)
            {
                query = query.Expression.Type == typeof(IOrderedQueryable<T>) ? ((IOrderedQueryable<T>)query).ThenBy(sortExpression) : query.OrderBy(sortExpression);
            }
            else
            {
                query = query.Expression.Type == typeof(IOrderedQueryable<T>) ? ((IOrderedQueryable<T>)query).ThenByDescending(sortExpression) : query.OrderByDescending(sortExpression);
            }

            return query;
        }
    }
}
