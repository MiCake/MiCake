using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace MiCake.Util.LinqFilter
{
    public static class LinqFilterExtensions
    {
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, List<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return query;
            }

            ParameterExpression pe = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filters, pe);

            if (expression == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = System.Linq.Expressions.Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expression, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> query, FilterGroup filterGroup)
        {
            if (filterGroup?.Filters == null || filterGroup.Filters.Count == 0)
            {
                return query;
            }

            ParameterExpression pe = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filterGroup.Filters, pe, filterGroup.FilterGroupJoinType);

            if (expression == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = System.Linq.Expressions.Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expression, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> query, CompositeFilterGroup filterGroupsHolder)
        {
            if (filterGroupsHolder.FilterGroups.Count == 0)
            {
                return query;
            }

            System.Linq.Expressions.Expression exp = null;
            ParameterExpression pe = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");

            foreach (var filterGroup in filterGroupsHolder.FilterGroups)
            {
                var filters = filterGroup.Filters;

                if (filters.Count == 0)
                {
                    continue;
                }

                var expression = CreateFilterExpression<T>(filters, pe, filterGroup.FilterGroupJoinType);

                if (exp == null)
                {
                    exp = expression;
                }
                else
                {
                    exp = ExpressionHelpers.ConcatExpressionsWithOperator(exp, expression, filterGroupsHolder.FilterGroupJoinType);
                }
            }

            MethodCallExpression whereCallExpression = System.Linq.Expressions.Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(exp, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static Expression<Func<T, bool>> GetFilterExpression<T>(this List<Filter> filters)
        {
            if (filters.Count == 0)
            {
                return null;
            }

            ParameterExpression pe = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filters, pe);
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expression, pe);
        }

        private static System.Linq.Expressions.Expression CreateFilterExpression<T>(List<Filter> filters, ParameterExpression pe, FilterJoinType filterGroupJoinType = FilterJoinType.And)
        {
            System.Linq.Expressions.Expression exp = null;
            foreach (var filter in filters)
            {
                System.Linq.Expressions.Expression left = ExpressionHelpers.BuildNestedPropertyExpression(pe, filter.PropertyName);
                System.Linq.Expressions.Expression builded = BuildFilterValuesExpression(left, filter.Value, filter.FilterValueJoinType);

                if (exp == null)
                {
                    exp = builded;
                }
                else
                {
                    exp = ExpressionHelpers.ConcatExpressionsWithOperator(exp, builded, filterGroupJoinType);
                }
            }

            return exp;
        }

        private static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) != null;

        private static IList RemakeStaticListWithNewType(Type newItemType, IList source)
        {
            var listType = typeof(List<>);
            Type[] typeArgs = [newItemType];
            var genericListType = listType.MakeGenericType(typeArgs);
            var typedList = (IList)Activator.CreateInstance(genericListType);
            foreach (var item in source)
            {
                if (IsNullable(newItemType))
                {
                    var underlyingType = Nullable.GetUnderlyingType(newItemType);
                    typedList.Add(System.Convert.ChangeType(item, underlyingType));
                }
                else
                {
                    typedList.Add(System.Convert.ChangeType(item, newItemType));
                }
            }
            return typedList;
        }

        private static System.Linq.Expressions.Expression BuildFilterValuesExpression(System.Linq.Expressions.Expression left, List<FilterValue> filterValues, FilterJoinType filterValueJoinType = FilterJoinType.Or)
        {
            System.Linq.Expressions.Expression exp = null;

            foreach (var filterValue in filterValues)
            {
                var valueType = filterValue.Value.GetType();
                System.Linq.Expressions.Expression right = null;
                if (valueType.IsGenericType && filterValue.Value is IList)
                {
                    IList list = RemakeStaticListWithNewType(left.Type, filterValue.Value as IList);
                    right = System.Linq.Expressions.Expression.Constant(list);
                }
                else
                {
                    if (IsNullable(left.Type))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(left.Type);
                        Type type = typeof(Nullable<>).MakeGenericType(underlyingType);
                        right = System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.Constant(System.Convert.ChangeType(filterValue.Value, underlyingType)), type);
                    }
                    else
                    {
                        object exceptValue;
                        if (valueType == typeof(string))
                        {
                            exceptValue = TypeDescriptor.GetConverter(left.Type).ConvertFromString(filterValue.Value.ToString());
                        }
                        else
                        {
                            exceptValue = System.Convert.ChangeType(filterValue.Value, left.Type);
                        }
                        right = System.Linq.Expressions.Expression.Constant(exceptValue);
                    }
                }

                System.Linq.Expressions.Expression concatenated = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, filterValue.Operator);
                if (exp == null)
                {
                    exp = concatenated;
                }
                else
                {
                    exp = ExpressionHelpers.ConcatExpressionsWithOperator(exp, concatenated, filterValueJoinType);
                }
            }

            return exp;
        }
    }
}