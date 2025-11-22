using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// Extension methods for sorting IQueryable instances.
    /// </summary>
    public static class SortingExtensions
    {
        /// <summary>
        /// Applies a list of sort specifications to an IQueryable.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The source queryable.</param>
        /// <param name="orders">The list of sort specifications to apply.</param>
        /// <returns>A sorted queryable.</returns>
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, IEnumerable<Sort> orders)
        {
            if (orders == null || !orders.Any())
            {
                return query;
            }

            foreach (var order in orders)
            {
                query = query.Sort(order);
            }
            return query;
        }

        /// <summary>
        /// Applies a sort specification to an IQueryable.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The source queryable.</param>
        /// <param name="order">The sort specification to apply.</param>
        /// <returns>A sorted queryable.</returns>
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, Sort order)
        {
            if (order == null)
            {
                return query;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            Expression property = ExpressionHelpers.BuildNestedPropertyExpression(pe, order.PropertyName);

            var sortExpression = Expression.Lambda<Func<T, object>>
                (Expression.Convert(property, typeof(object)), pe);

            // Check if the current query is already ordered
            if (query is IOrderedQueryable<T> orderedQuery)
            {
                query = order.Ascending
                    ? orderedQuery.ThenBy(sortExpression)
                    : orderedQuery.ThenByDescending(sortExpression);
            }
            else
            {
                query = order.Ascending
                    ? query.OrderBy(sortExpression)
                    : query.OrderByDescending(sortExpression);
            }

            return query;
        }
    }
}
