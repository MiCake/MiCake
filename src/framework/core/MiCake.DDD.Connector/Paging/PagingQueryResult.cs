namespace MiCake.DDD.Connector.Paging
{
    /// <summary>
    /// A model for paging query result.
    /// </summary>
    public class PagingQueryResult
    {
        public long TotalCount { get; set; }

        public int CurrentIndex { get; set; }

        public PagingQueryResult(int currentIndex, long total)
        {
            CurrentIndex = currentIndex;
            TotalCount = total;
        }
    }

    /// <summary>
    /// A model for paging query result.(include custom data)
    /// </summary>
    public class PagingQueryResult<T> : PagingQueryResult
    {
        public T Data { get; set; }

        public PagingQueryResult(int currentIndex, long total, T data) : base(currentIndex, total)
        {
            Data = data;
        }
    }
}
