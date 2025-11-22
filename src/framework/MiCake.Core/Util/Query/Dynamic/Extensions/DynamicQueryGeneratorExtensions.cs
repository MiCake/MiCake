using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MiCake.Util.Query.Dynamic;

/// <summary>
/// Extension methods for generating filter groups from objects decorated with <see cref="DynamicFilterAttribute"/>.
/// </summary>
public static class DynamicQueryGeneratorExtensions
{
    /// <summary>
    /// Generates a filter group from an object's properties decorated with <see cref="DynamicFilterAttribute"/>.
    /// </summary>
    /// <typeparam name="T">The type of the query object that implements <see cref="IDynamicQueryObj"/>.</typeparam>
    /// <param name="queryObj">The query object to generate filters from.</param>
    /// <returns>
    /// A <see cref="FilterGroup"/> containing filters for all non-null properties 
    /// decorated with <see cref="DynamicFilterAttribute"/>.
    /// </returns>
    /// <remarks>
    /// Only properties with values are included in the generated filter group.
    /// Null values and empty collections are skipped.
    /// </remarks>
    public static FilterGroup GenerateFilterGroup<T>(this T queryObj) where T : IDynamicQueryObj
    {
        var group = new FilterGroup();

        var joinAttr = queryObj.GetType().GetCustomAttribute<DynamicFilterJoinAttribute>();
        var joinType = joinAttr?.JoinType ?? FilterJoinType.And;

        foreach (var property in queryObj.GetType().GetProperties())
        {
            var attr = property.GetCustomAttribute<DynamicFilterAttribute>();
            if (attr == null) continue;

            var value = property.GetValue(queryObj);
            if (!IsValueValid(value)) continue;

            group.FiltersJoinType = joinType;
            var propertyName = attr.PropertyName ?? property.Name;
            group.Filters.Add(CreateFilter(propertyName, value!, attr));
        }
        return group;
    }

    /// <summary>
    /// Determines whether a value is valid for filter generation.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>
    /// true if the value is not null and not an empty collection; otherwise false.
    /// Note: Zero (0) and false are considered valid values.
    /// </returns>
    private static bool IsValueValid(object? value)
    {
        // null values are always invalid
        if (value == null) return false;
        // empty or whitespace strings are considered invalid
        if (value is string s)
            return !string.IsNullOrWhiteSpace(s);

        // For enumerables (except string), check if they contain any elements
        if (value is IEnumerable enumerable && value is not string)
        {
            // Avoid materializing expensive enumerables. Use IEnumerable.GetEnumerator()
            var enumerator = enumerable.GetEnumerator();
            try
            {
                return enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
        
        // all other values (including 0, false, empty Guid) are valid
        return true;
    }

    private static Filter CreateFilter(string propertyName, object value, DynamicFilterAttribute attr)
    {
        if (value is ICollection collection && value is not string)
        {
            var filterValues = new List<FilterValue>();
            foreach (var item in collection)
            {
                if (item != null)
                    filterValues.Add(FilterValue.Create(item, attr.OperatorType));
            }
            return Filter.Create(propertyName, filterValues);
        }
        return Filter.Create(propertyName, [FilterValue.Create(value, attr.OperatorType)]);
    }
}
