using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Security;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// Extension methods for filtering IQueryable instances using dynamic filter expressions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides extension methods to apply <see cref="T:MiCake.Util.Query.Dynamic.Filter"/>, <see cref="FilterGroup"/>, 
    /// and <see cref="CompositeFilterGroup"/> objects to LINQ queries.
    /// </para>
    /// <para>
    /// It also provides methods to extract filter expressions for external use.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var filters = new List&lt;Filter&gt;
    /// {
    ///     Filter.Create("Age", new List&lt;FilterValue&gt; { FilterValue.Create(18, FilterOperatorType.GreaterThan) }),
    ///     Filter.Create("Name", new List&lt;FilterValue&gt; { FilterValue.Create("John", FilterOperatorType.Contains) })
    /// };
    /// 
    /// var results = dbContext.Users.Filter(filters).ToList();
    /// </code>
    /// </para>
    /// </remarks>
    public static class FilterExtensions
    {
        /// <summary>
        /// Applies a list of filters to an IQueryable using AND logic (all filters must match).
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The source queryable.</param>
        /// <param name="filters">The list of filters to apply.</param>
        /// <returns>A filtered queryable.</returns>
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, IEnumerable<Filter> filters)
        {
            if (filters == null || !filters.Any())
            {
                return query;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filters, pe);

            return ApplyFilter(query, expression, pe);
        }

        /// <summary>
        /// Applies a filter group to an IQueryable using the specified join type.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The source queryable.</param>
        /// <param name="filterGroup">The filter group to apply.</param>
        /// <returns>A filtered queryable.</returns>
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, FilterGroup filterGroup)
        {
            if (filterGroup?.Filters == null || filterGroup.Filters.Count == 0)
            {
                return query;
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            var expression = CreateFilterExpression<T>(filterGroup.Filters, pe, filterGroup.FiltersJoinType);

            return ApplyFilter(query, expression, pe);
        }

        /// <summary>
        /// Applies a composite filter group (multiple filter groups) to an IQueryable.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The source queryable.</param>
        /// <param name="filterGroupsHolder">The composite filter group to apply.</param>
        /// <returns>A filtered queryable.</returns>
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, CompositeFilterGroup filterGroupsHolder)
        {
            if (filterGroupsHolder?.FilterGroups == null || filterGroupsHolder.FilterGroups.Count == 0)
            {
                return query;
            }

            Expression? exp = null;
            ParameterExpression pe = Expression.Parameter(typeof(T), "x");

            foreach (var filterGroup in filterGroupsHolder.FilterGroups)
            {
                if (filterGroup.Filters.Count == 0)
                {
                    continue;
                }

                var expression = CreateFilterExpression<T>(filterGroup.Filters, pe, filterGroup.FiltersJoinType);
                exp = CombineExpressions(exp, expression, filterGroupsHolder.FilterGroupsJoinType);
            }

            return ApplyFilter(query, exp, pe);
        }

        /// <summary>
        /// Generates a compiled filter expression from a list of filters.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="filters">The list of filters to generate expression from.</param>
        /// <returns>A compiled lambda expression, or null if the filters list is empty.</returns>
        public static Expression<Func<T, bool>>? GetFilterExpression<T>(this IEnumerable<Filter> filters)
        {
            if (filters == null || !filters.Any())
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

        private static IQueryable<T> ApplyFilter<T>(IQueryable<T> query, Expression? expression, ParameterExpression parameter)
        {
            if (expression == null)
            {
                return query;
            }

            MethodCallExpression whereCallExpression = Expression.Call(
               typeof(Queryable),
               "Where",
               [query.ElementType],
               query.Expression,
               Expression.Lambda<Func<T, bool>>(expression, [parameter]));

            return query.Provider.CreateQuery<T>(whereCallExpression);
        }

        private static Expression? CreateFilterExpression<T>(IEnumerable<Filter> filters, ParameterExpression parameter, FilterJoinType filterGroupJoinType = FilterJoinType.And)
        {
            Expression? combined = null;
            foreach (var filter in filters)
            {
                var left = ExpressionHelpers.BuildNestedPropertyExpression(parameter, filter.PropertyName);
                var built = BuildFilterValuesExpression(left, filter.Values, filter.ValuesJoinType);
                combined = CombineExpressions(combined, built, filterGroupJoinType);
            }

            return combined;
        }

        private static Expression? CombineExpressions(Expression? current, Expression? next, FilterJoinType joinType)
        {
            if (next == null)
            {
                return current;
            }

            return current == null
                ? next
                : ExpressionHelpers.ConcatExpressionsWithOperator(current, next, joinType);
        }

        private static Expression? BuildFilterValuesExpression(Expression left, IEnumerable<FilterValue> filterValues, FilterJoinType filterValueJoinType = FilterJoinType.Or)
        {
            Expression? combined = null;

            foreach (var filterValue in filterValues)
            {
                var singleExpression = BuildSingleFilterExpression(left, filterValue);
                combined = CombineExpressions(combined, singleExpression, filterValueJoinType);
            }

            return combined;
        }

        private static Expression BuildSingleFilterExpression(Expression left, FilterValue filterValue)
        {
            EnsurePropertyAllowed(left);
            ValidateFilterValue(filterValue, left.Type);

            var right = BuildRightExpression(left, filterValue);
            return ExpressionHelpers.ConcatExpressionsWithOperator(left, right, filterValue.Operator);
        }

        private static Expression BuildRightExpression(Expression left, FilterValue filterValue)
        {
            if (filterValue.Value is IList listValue)
            {
                var typedList = FilterValueConversion.ToTypedList(left.Type, listValue);
                return Expression.Constant(typedList);
            }

            if (filterValue.Operator == ValueOperatorType.In)
            {
                var elementType = FilterValueConversion.ExtractElementType(left.Type);
                var typedSingleList = FilterValueConversion.ToTypedList(elementType, new ArrayList { filterValue.Value });

                if (FilterValueConversion.IsNullable(left.Type))
                {
                    var nullableList = FilterValueConversion.ToTypedList(left.Type, typedSingleList);
                    return Expression.Constant(nullableList);
                }

                return Expression.Constant(typedSingleList);
            }

            var targetType = left.Type;
            if (FilterValueConversion.IsNullable(targetType))
            {
                var underlying = Nullable.GetUnderlyingType(targetType)!;
                var nullableType = typeof(Nullable<>).MakeGenericType(underlying);
                var converted = FilterValueConversion.ConvertValue(filterValue.Value, underlying);
                return Expression.Constant(converted, nullableType);
            }

            var value = FilterValueConversion.ConvertValue(filterValue.Value, targetType);
            return Expression.Constant(value, targetType);
        }

        private static void EnsurePropertyAllowed(Expression left)
        {
            if (!IsPropertyAllowed(left))
            {
                throw new SecurityException("Attempt to filter on disallowed member or method call detected.");
            }
        }

        private static bool IsPropertyAllowed(Expression left)
        {
            if (left is MemberExpression me && me.Member is System.Reflection.PropertyInfo prop)
            {
                return prop.GetMethod != null && prop.GetMethod.IsPublic;
            }
            return false;
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

        private static class FilterValueConversion
        {
            public static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) != null;

            public static Type ExtractElementType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

            public static object? ConvertValue(object? value, Type targetType)
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
                        return System.Convert.ChangeType(s, underlying);
                    }

                    if (underlying.IsInstanceOfType(value))
                        return value;

                    return System.Convert.ChangeType(value, underlying);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to convert filter value '{value}' to type '{underlying.Name}'", ex);
                }
            }

            public static IList ToTypedList(Type targetType, IList source)
            {
                var listType = typeof(List<>).MakeGenericType(targetType);
                var typedList = Activator.CreateInstance(listType) as IList ?? throw new InvalidOperationException("Failed to create typed list.");

                foreach (var item in source)
                {
                    typedList.Add(ConvertValue(item, targetType));
                }

                return typedList;
            }
        }
    }
}