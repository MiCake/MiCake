using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Security;

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
                object? converted;
                if (IsNullable(newItemType))
                {
                    var underlyingType = Nullable.GetUnderlyingType(newItemType)!;
                    converted = ConvertValueSafely(item, underlyingType);
                }
                else
                {
                    converted = ConvertValueSafely(item, newItemType);
                }

                typedList.Add(converted);
            }

            return typedList;
        }

        private static bool IsPropertyAllowed(Expression left)
        {
            if (left is MemberExpression me && me.Member is System.Reflection.PropertyInfo prop)
            {
                return prop.GetMethod != null && prop.GetMethod.IsPublic;
            }
            return false;
        }

        private static object? ConvertValueSafely(object? value, Type targetType)
        {
            if (value == null)
                return null;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (value is string s)
                {
                    var converter = TypeDescriptor.GetConverter(underlying);
                    if (converter != null && converter.CanConvertFrom(typeof(string)))
                    {
                        return converter.ConvertFromString(s);
                    }
                    // Fall back to ChangeType
                    return System.Convert.ChangeType(s, underlying);
                }

                // Already correct type? return directly
                if (underlying.IsInstanceOfType(value))
                    return value;

                return System.Convert.ChangeType(value, underlying);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert filter value '{value}' to type '{underlying.Name}'", ex);
            }
        }

        private static Expression? BuildFilterValuesExpression(Expression left, List<FilterValue> filterValues, FilterJoinType filterValueJoinType = FilterJoinType.Or)
        {
            Expression? exp = null;

            foreach (var filterValue in filterValues)
            {
                // Ensure the left expression targets an allowed property
                if (!IsPropertyAllowed(left))
                {
                    throw new SecurityException("Attempt to filter on disallowed member or method call detected.");
                }

                ValidateFilterValue(filterValue, left.Type);

                var valueType = filterValue.Value?.GetType();
                Expression? right = null;

                // Handle list / collection values
                if (filterValue.Value is IList list1)
                {
                    IList list = RemakeStaticListWithNewType(left.Type, list1);
                    right = Expression.Constant(list);
                }
                else
                {
                    if (IsNullable(left.Type))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(left.Type)!;
                        var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);

                        var convertedValue = ConvertValueSafely(filterValue.Value, underlyingType);

                        right = Expression.Constant(convertedValue, nullableType);
                    }
                    else
                    {
                        var converted = ConvertValueSafely(filterValue.Value, left.Type);
                        right = Expression.Constant(converted, left.Type);
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