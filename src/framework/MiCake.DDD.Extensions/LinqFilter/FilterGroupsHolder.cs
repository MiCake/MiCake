using MiCake.DDD.Extensions.LinqFilter;
using System.Collections.Generic;

namespace Calliope.Dream.Infrastructure.LinqFilter
{
    public class FilterGroupsHolder
    {
        public List<FilterGroup> FilterGroups { get; set; } = new List<FilterGroup>();

        public FilterJoinType FilterGroupJoinType { get; set; }
    }
}
