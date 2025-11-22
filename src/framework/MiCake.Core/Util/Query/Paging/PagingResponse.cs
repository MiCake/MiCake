using System.Collections.Generic;

namespace MiCake.Util.Query.Paging
{
    /// <summary>
    /// Represents a paging response with total count and current index.
    /// </summary>
    public class PagingResponse
    {
        /// <summary>
        /// Gets or sets the total count of items.
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page index.
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingResponse"/> class.
        /// </summary>
        /// <param name="currentIndex">The current page index.</param>
        /// <param name="total">The total count of items.</param>
        public PagingResponse(int currentIndex, long total)
        {
            CurrentIndex = currentIndex;
            TotalCount = total;
        }
    }

    /// <summary>
    /// Represents a paging response with data.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    public class PagingResponse<T> : PagingResponse
    {
        /// <summary>
        /// Gets or sets the collection of data items.
        /// </summary>
        public IEnumerable<T> Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingResponse{T}"/> class.
        /// </summary>
        /// <param name="currentIndex">The current page index.</param>
        /// <param name="total">The total count of items.</param>
        /// <param name="data">The collection of data items.</param>
        public PagingResponse(int currentIndex, long total, IEnumerable<T> data) : base(currentIndex, total)
        {
            Data = data;
        }
    }
}
