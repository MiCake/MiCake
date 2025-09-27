namespace MiCake.Core.Util.PaginationData
{
    /// <summary>
    /// Pagination request context
    /// </summary>
    public class PaginationRequest<TRequest>
    {
        /// <summary>
        /// Request data
        /// </summary>
        public TRequest Request { get; set; } = default!;

        /// <summary>
        /// Offset to start retrieving items from
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Number of items to retrieve in this request
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Optional identifier for logging or tracking purposes
        /// </summary>
        public string? Identifier { get; set; }
    }
}