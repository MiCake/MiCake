#nullable disable warnings

using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MiCake.Util.LinqFilter;

public static class DynamicQueryGeneratorExtensions
{
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

            group.FilterGroupJoinType = joinType;
            var propertyName = attr.PropertyName ?? property.Name;
            group.Filters.Add(CreateFilter(propertyName, value!, attr));
        }
        return group;
    }

    private static bool IsValueValid(object? value)
    {
        if (value == null) return false;
        if (value is ICollection collection) return collection.Count > 0;
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
