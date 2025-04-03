using System.Collections.Generic;

namespace MiCake.Core.Util.LinqFilter
{
    public class FilterGroupsHolder
    {
        public List<FilterGroup> FilterGroups { get; set; } = [];

        public FilterJoinType FilterGroupJoinType { get; set; }
    }
}
