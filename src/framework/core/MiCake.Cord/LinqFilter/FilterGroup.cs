namespace MiCake.Cord.LinqFilter
{
    public class FilterGroup
    {
        public List<Filter> Filters { get; set; } = new List<Filter>();

        public FilterJoinType FilterGroupJoinType { get; set; } = FilterJoinType.Or;
    }
}
