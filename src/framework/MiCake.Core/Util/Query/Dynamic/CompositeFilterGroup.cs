using System.Collections.Generic;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// A composite filter group that combines multiple <see cref="FilterGroup"/> instances.
    /// <para>
    /// This is useful for scenarios where you need to group multiple filter groups together,
    /// allowing for complex filtering logic that can be applied to a query.
    /// </para>
    /// </summary>
    public class CompositeFilterGroup
    {
        public List<FilterGroup> FilterGroups { get; set; } = [];

        public FilterJoinType FilterGroupJoinType { get; set; }

        public static CompositeFilterGroup Create(List<FilterGroup> filterGroups, FilterJoinType filterGroupJoinType = FilterJoinType.Or)
        {
            if (filterGroups == null || filterGroups.Count == 0)
            {
                throw new System.ArgumentException("Filter groups cannot be null or empty.", nameof(filterGroups));
            }

            return new CompositeFilterGroup
            {
                FilterGroups = filterGroups,
                FilterGroupJoinType = filterGroupJoinType
            };
        }
    }
}
