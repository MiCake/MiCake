using Calliope.Dream.Infrastructure.LinqFilter;
using System.Collections.Generic;

namespace MiCake.DDD.Extensions.LinqFilter
{
    public class FilterGroup
    {
        public List<Filter> Filters { get; set; } = new List<Filter>();

        public FilterJoinType FilterGroupJoinType { get; set; } = FilterJoinType.Or;
    }
}
