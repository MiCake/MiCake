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
        public List<Filter> Filters { get; set; } = [];

        public FilterJoinType FilterGroupJoinType { get; set; } = FilterJoinType.Or;

        public static FilterGroup Create(List<Filter> filters, FilterJoinType filterGroupJoinType = FilterJoinType.Or)
        {
            if (filters == null || filters.Count == 0)
            {
                throw new System.ArgumentException("Filter list cannot be null or empty.", nameof(filters));
            }

            return new FilterGroup
            {
                Filters = filters,
                FilterGroupJoinType = filterGroupJoinType
            };
        }
    }
}
