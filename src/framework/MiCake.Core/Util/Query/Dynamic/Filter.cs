using System;
using System.Collections.Generic;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// A filter that represents a single property and its associated values for filtering.
    /// <para>
    /// This is used to define how a specific property should be filtered in a query.
    /// </para>
    /// </summary>
    public class Filter
    {
        /// <summary>
        /// The property name on the entity that this filter applies to. Supports nested properties with dot notation (eg. "Address.City").
        /// </summary>
        public required string PropertyName { get; set; }

        /// <summary>
        /// The list of <see cref="FilterValue"/> instances representing values to test for this property.
        /// Use multiple values to create combined checks (see <see cref="ValuesJoinType"/>).
        /// </summary>
        public List<FilterValue> Values { get; set; } = [];

        /// <summary>
        /// How multiple values on this property are combined (AND / OR).
        /// Defaults to <see cref="FilterJoinType.Or"/>.
        /// </summary>
        public FilterJoinType ValuesJoinType { get; set; } = FilterJoinType.Or;

        /// <summary>
        /// Factory helper to create a Filter instance with validation.
        /// </summary>
        /// <param name="propertyName">The property that should be filtered (non-empty).</param>
        /// <param name="values">A non-empty collection of FilterValue instances.</param>
        /// <param name="valuesJoinType">How the provided values are combined (AND/OR).</param>
        /// <returns>A validated <see cref="Filter"/> instance.</returns>
        public static Filter Create(string propertyName, List<FilterValue> values, FilterJoinType valuesJoinType = FilterJoinType.Or)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
            }

            if (values == null || values.Count == 0)
            {
                throw new ArgumentException("Filter values cannot be null or empty.", nameof(values));
            }

            return new Filter
            {
                PropertyName = propertyName,
                Values = values ?? [],
                ValuesJoinType = valuesJoinType
            };
        }
    }
}
