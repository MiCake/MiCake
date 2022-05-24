namespace MiCake.Cord.Paging
{
    /// <summary>
    /// A model for paging query.
    /// </summary>
    public class PaginationFilter
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public PaginationFilter(int pageIndex, int pageSize)
        {
            if (pageIndex < 0 || pageSize < 0)
                throw new ArgumentException("page index and page num cannot less than zero.");

            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public int CurrentStartNo => (PageIndex - 1) * PageSize;

    }

    /// <summary>
    /// A model for paging query.(include custom data)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginationFilter<T> : PaginationFilter
    {
        public PaginationFilter(int pageIndex, int pageSize, T data) : base(pageIndex, pageSize)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
