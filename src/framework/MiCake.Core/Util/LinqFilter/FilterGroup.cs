using System.Collections.Generic;

namespace MiCake.Core.Util.LinqFilter
{
    public class FilterGroup
    {
        public List<Filter> Filters { get; set; } = [];

        public FilterJoinType FilterGroupJoinType { get; set; } = FilterJoinType.Or;
    }
}
