namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// A filter value that represents a single value and its associated operator for filtering.
    /// <para>
    /// This is used to define how a specific value should be filtered in a query.
    /// </para>
    /// </summary>
    public class FilterValue
    {
        public required object Value { get; set; }

        public FilterOperatorType Operator { get; set; }

        public static FilterValue Create(object value, FilterOperatorType filterOperator)
        {
            return new FilterValue
            {
                Value = value,
                Operator = filterOperator
            };
        }
    }
}
