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

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filters, pe);

            if (expression == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               Expression.Lambda<Func<T, bool>>(expression, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> query, FilterGroup filterGroup)
        {
            if (filterGroup?.Filters == null || filterGroup.Filters.Count == 0)
            {
                return query;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filterGroup.Filters, pe, filterGroup.FilterGroupJoinType);

            if (expression == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               Expression.Lambda<Func<T, bool>>(expression, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> query, CompositeFilterGroup filterGroupsHolder)
        {
            if (filterGroupsHolder.FilterGroups.Count == 0)
            {
                return query;
            }

            Expression? exp = null;
            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

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
                    exp = ExpressionHelpers.ConcatExpressionsWithOperator(exp, expression!, filterGroupsHolder.FilterGroupJoinType);
                }
            }

            if (exp == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               Expression.Lambda<Func<T, bool>>(exp, [pe]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        public static Expression<Func<T, bool>>? GetFilterExpression<T>(this List<Filter> filters)
        {
            if (filters.Count == 0)
            {
                return null;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filters, pe);
            if (expression == null)
            {
                return null;
            }
            return Expression.Lambda<Func<T, bool>>(expression, pe);
        }

        private static Expression? CreateFilterExpression<T>(List<Filter> filters, ParameterExpression pe, FilterJoinType filterGroupJoinType = FilterJoinType.And)
        {
            Expression? exp = null;
            foreach (var filter in filters)
            {
                Expression left = ExpressionHelpers.BuildNestedPropertyExpression(pe, filter.PropertyName);
                Expression? builded = BuildFilterValuesExpression(left, filter.Value, filter.FilterValueJoinType);

                if (exp == null)
                {
                    exp = builded;
                }
                else
                {
                    exp = ExpressionHelpers.ConcatExpressionsWithOperator(exp, builded!, filterGroupJoinType);
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
            var typedList = Activator.CreateInstance(genericListType) as IList ?? throw new InvalidOperationException("Failed to create typed list.");

            foreach (var item in source)
            {
                if (IsNullable(newItemType))
                {
                    var underlyingType = Nullable.GetUnderlyingType(newItemType);
                    typedList.Add(System.Convert.ChangeType(item, underlyingType!));
                }
                else
                {
                    typedList.Add(System.Convert.ChangeType(item, newItemType));
                }
            }
            return typedList;
        }

        private static Expression? BuildFilterValuesExpression(Expression left, List<FilterValue> filterValues, FilterJoinType filterValueJoinType = FilterJoinType.Or)
        {
            Expression? exp = null;

            foreach (var filterValue in filterValues)
            {
                ValidateFilterValue(filterValue, left.Type);

                var valueType = filterValue.Value?.GetType();
                Expression? right = null;
                if (valueType?.IsGenericType is true && filterValue.Value is IList list1)
                {
                    IList list = RemakeStaticListWithNewType(left.Type, list1);
                    right = Expression.Constant(list);
                }
                else
                {
                    if (IsNullable(left.Type))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(left.Type);
                        Type type = typeof(Nullable<>).MakeGenericType(underlyingType!);

                        object? convertedValue;
                        try
                        {
                            convertedValue = System.Convert.ChangeType(filterValue.Value, underlyingType!);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Failed to convert filter value '{filterValue.Value}' to type '{underlyingType!.Name}'", ex);
                        }

                        right = Expression.Convert(Expression.Constant(convertedValue), type);
                    }
                    else
                    {
                        object? exceptValue;
                        try
                        {
                            if (valueType == typeof(string))
                            {
                                exceptValue = TypeDescriptor.GetConverter(left.Type)?.ConvertFromString(filterValue.Value?.ToString() ?? string.Empty);
                            }
                            else
                            {
                                exceptValue = System.Convert.ChangeType(filterValue.Value, left.Type);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Failed to convert filter value '{filterValue.Value}' to type '{left.Type.Name}'", ex);
                        }
                        right = Expression.Constant(exceptValue);
                    }
                }

                Expression concatenated = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, filterValue.Operator);
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

        private static void ValidateFilterValue(FilterValue filterValue, Type targetType)
        {
            if (filterValue.Value == null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    throw new ArgumentException($"Cannot convert null to non-nullable type {targetType.Name}");
                }
                return;
            }
        }
    }
}