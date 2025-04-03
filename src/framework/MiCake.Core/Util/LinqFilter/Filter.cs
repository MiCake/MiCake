using System.Collections.Generic;

namespace MiCake.Core.Util.LinqFilter
{
    public class Filter
    {
        public string PropertyName { get; set; }

        public List<FilterValue> Value { get; set; } = [];

        public FilterJoinType FilterValueJoinType { get; set; } = FilterJoinType.Or;
    }
}
