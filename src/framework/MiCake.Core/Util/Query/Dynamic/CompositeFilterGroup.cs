using System;
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
        /// <summary>
        /// Groups that are combined inside this composite holder.
        /// </summary>
        public List<FilterGroup> FilterGroups { get; set; } = [];

        /// <summary>
        /// How the groups inside this composite are combined (AND/OR).
        /// Default is <see cref="FilterJoinType.Or"/> for backward compatibility.
        /// </summary>
        public FilterJoinType FilterGroupsJoinType { get; set; } = FilterJoinType.Or;

        /// <summary>
        /// Create a new <see cref="CompositeFilterGroup"/> with validation.
        /// </summary>
        /// <param name="filterGroups">Non-empty list of filter groups.</param>
        /// <param name="filterGroupsJoinType">How the groups are combined in this composite (defaults to <see cref="FilterJoinType.And"/>).</param>
        public static CompositeFilterGroup Create(List<FilterGroup> filterGroups, FilterJoinType filterGroupsJoinType = FilterJoinType.Or)
        {
            if (filterGroups == null || filterGroups.Count == 0)
            {
                throw new ArgumentException("Filter groups cannot be null or empty.", nameof(filterGroups));
            }

            return new CompositeFilterGroup
            {
                FilterGroups = filterGroups,
                FilterGroupsJoinType = filterGroupsJoinType
            };
        }
    }
}
