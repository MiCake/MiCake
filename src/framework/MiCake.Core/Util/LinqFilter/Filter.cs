using System.Collections.Generic;

namespace MiCake.Core.Util.LinqFilter
{
    /// <summary>
    /// A filter that represents a single property and its associated values for filtering.
    /// <para>
    /// This is used to define how a specific property should be filtered in a query.
    /// </para>
    /// </summary>
    public class Filter
    {
        public string PropertyName { get; set; }

        public List<FilterValue> Value { get; set; } = [];

        public FilterJoinType FilterValueJoinType { get; set; } = FilterJoinType.Or;

        public static Filter Create(string propertyName, List<FilterValue> values, FilterJoinType filterValueJoinType = FilterJoinType.Or)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new System.ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
            }

            if (values == null || values.Count == 0)
            {
                throw new System.ArgumentException("Filter values cannot be null or empty.", nameof(values));
            }

            return new Filter
            {
                PropertyName = propertyName,
                Value = values ?? [],
                FilterValueJoinType = filterValueJoinType
            };
        }
    }
}
