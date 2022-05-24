namespace MiCake.Cord.Paging
{
    /// <summary>
    /// A model for paging query result.
    /// </summary>
    public class PagingQueryResult
    {
        public long TotalCount { get; set; }

        public int CurrentPageNumber { get; set; }

        public PagingQueryResult(int currentPageNumber, long total)
        {
            CurrentPageNumber = currentPageNumber;
            TotalCount = total;
        }
    }

    /// <summary>
    /// A model for paging query result.(include custom data)
    /// </summary>
    public class PagingQueryResult<T> : PagingQueryResult
    {
        public List<T> Data { get; set; }

        public PagingQueryResult(int currentPageNumber, long total, List<T> data) : base(currentPageNumber, total)
        {
            Data = data;
        }

        public static PagingQueryResult<T> Empty(PaginationFilter filter)
        {
            return Empty(filter.PageIndex);
        }

        /// <summary>
        /// return a empty paging query result.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public static PagingQueryResult<T> Empty(int pageNumber)
        {
            return new PagingQueryResult<T>(pageNumber, 0, new List<T>());
        }

        public static PagingQueryResult<T> Result(PaginationFilter filter, long total, List<T> data)
        {
            return new PagingQueryResult<T>(filter.PageIndex, total, data);
        }

        public static PagingQueryResult<T> Result(int pageNumber, long total, List<T> data)
        {
            return new PagingQueryResult<T>(pageNumber, total, data);
        }
    }
}
