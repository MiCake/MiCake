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
        public T Data { get; set; }

        public PagingQueryResult(int currentPageNumber, long total, T data) : base(currentPageNumber, total)
        {
            Data = data;
        }
    }
}
