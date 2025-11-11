using System;

namespace MiCake.Util.Paging
{
    /// <summary>
    /// Represents a model for paging query parameters.
    /// </summary>
    public class PagingRequest
    {
        /// <summary>
        /// Gets or sets the page index
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size (number of items per page).
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingRequest"/> class.
        /// </summary>
        /// <param name="pageIndex">The page index.</param>
        /// <param name="pageSize">The page size.</param>
        /// <exception cref="ArgumentException">Thrown when pageIndex or pageSize is less than zero.</exception>
        public PagingRequest(int pageIndex, int pageSize)
        {
            if (pageIndex < 0 || pageSize < 0)
                throw new ArgumentException("page index and page num cannot less than zero.");

            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        /// <summary>
        /// Gets the starting number of the current page (1-based).
        /// </summary>
        public int CurrentStartNo => (PageIndex - 1) * PageSize;
    }

    /// <summary>
    /// Represents a paging query model with additional data.
    /// </summary>
    /// <typeparam name="T">The type of the additional data.</typeparam>
    public class PagingRequest<T>(int pageIndex, int pageSize, T data) : PagingRequest(pageIndex, pageSize)
    {
        /// <summary>
        /// Gets or sets the additional data.
        /// </summary>
        public T Data { get; set; } = data;
    }
}
