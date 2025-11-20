#nullable disable warnings

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MiCake.Util.LinqFilter
{
    public static class SortingExtensions
    {
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, List<Sort> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return query;
            }

            foreach (var order in orders)
            {
                query = query.Sort(order);
            }
            return query;
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> query, Sort order)
        {
            if (order == null)
            {
                return query;
            }

            ParameterExpression pe = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");

            System.Linq.Expressions.Expression property = ExpressionHelpers.BuildNestedPropertyExpression(pe, order.PropertyName);

            var sortExpression = System.Linq.Expressions.Expression.Lambda<Func<T, object>>
                (System.Linq.Expressions.Expression.Convert(property, typeof(object)), pe);

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
