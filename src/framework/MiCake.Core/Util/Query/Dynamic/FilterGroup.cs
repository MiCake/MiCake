using System;
using System.Collections.Generic;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// A filter group that contains a list of filters.
    /// <para>
    /// This is used to group multiple filters together,
    /// allowing for complex filtering logic that can be applied to a query.
    /// </para>
    /// </summary>
    public class FilterGroup
    {
        /// <summary>
        /// The list of filters contained in this group.
        /// </summary>
        public List<Filter> Filters { get; set; } = [];

        /// <summary>
        /// How filters inside this group are combined (AND / OR).
        /// Default is <see cref="FilterJoinType.Or"/> to preserve historical behavior.
        /// </summary>
        public FilterJoinType FiltersJoinType { get; set; } = FilterJoinType.Or;

        /// <summary>
        /// Create a new <see cref="FilterGroup"/> with validation.
        /// </summary>
        /// <param name="filters">Non-empty list of filters.</param>
        /// <param name="filtersJoinType">How the filters are combined in this group. Defaults to <see cref="FilterJoinType.Or"/> for backward compatibility.</param>
        public static FilterGroup Create(List<Filter> filters, FilterJoinType filtersJoinType = FilterJoinType.Or)
        {
            if (filters == null || filters.Count == 0)
            {
                throw new ArgumentException("Filter list cannot be null or empty.", nameof(filters));
            }

            return new FilterGroup
            {
                Filters = filters,
                FiltersJoinType = filtersJoinType
            };
        }
    }
}
