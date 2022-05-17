namespace MiCake.Cord.LinqFilter
{
    public class Filter
    {
        public string? PropertyName { get; set; }

        public List<FilterValue> Value { get; set; } = new List<FilterValue>();

        public FilterJoinType FilterValueJoinType { get; set; } = FilterJoinType.Or;
    }
}
