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
        /// <summary>
        /// Gets or sets the filter value. Null is allowed and is used to express a null comparison when appropriate.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator to apply to the value.
        /// </summary>
        public ValueOperatorType Operator { get; set; }

        /// <summary>
        /// Creates a new FilterValue instance with validation.
        /// </summary>
        /// <param name="value">The filter value. Cannot be null.</param>
        /// <param name="filterOperator">The operator to apply.</param>
        /// <returns>A new FilterValue instance.</returns>
        /// <returns>A new FilterValue instance.</returns>
        public static FilterValue Create(object? value, ValueOperatorType filterOperator)
        {
            return new FilterValue
            {
                Value = value,
                Operator = filterOperator
            };
        }
    }
}
