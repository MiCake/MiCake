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

            // Check if the current query expression tree already contains an OrderBy/OrderByDescending call.
            if (HasOrderingMethodCall(query.Expression))
            {
                var orderedQuery = (IOrderedQueryable<T>)query;
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

        /// <summary>
        /// Checks if the expression tree contains an OrderBy/OrderByDescending method call,
        /// indicating that the query has already been sorted.
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <returns>True if the expression contains an ordering method call; otherwise, false.</returns>
        private static bool HasOrderingMethodCall(Expression expression)
        {
            if (expression is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;
                if (methodName == nameof(Queryable.OrderBy) ||
                    methodName == nameof(Queryable.OrderByDescending) ||
                    methodName == nameof(Queryable.ThenBy) ||
                    methodName == nameof(Queryable.ThenByDescending))
                {
                    return true;
                }

                // Recursively check the source expression (first argument for extension methods)
                if (methodCall.Arguments.Count > 0)
                {
                    return HasOrderingMethodCall(methodCall.Arguments[0]);
                }
            }

            return false;
        }
    }
}
