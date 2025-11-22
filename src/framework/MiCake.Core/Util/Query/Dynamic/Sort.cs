namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// Represents a sorting specification for a query result.
    /// </summary>
    public class Sort
    {
        /// <summary>
        /// Gets or sets the name of the property to sort by.
        /// Supports nested properties using dot notation (e.g., "Address.City").
        /// </summary>
        public required string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sort in ascending order.
        /// Default is true (ascending). Set to false for descending order.
        /// </summary>
        public bool Ascending { get; set; } = true;
    }
}
