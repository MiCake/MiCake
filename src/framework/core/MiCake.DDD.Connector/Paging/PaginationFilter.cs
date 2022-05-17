namespace MiCake.Cord.Paging
{
    /// <summary>
    /// A model for paging query.
    /// </summary>
    public class PaginationFilter
    {
        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public PaginationFilter(int pageNumber, int pageSize)
        {
            if (pageNumber < 0 || pageSize < 0)
                throw new ArgumentException("page index and page num cannot less than zero.");

            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public int CurrentStartNo => (PageNumber - 1) * PageSize;

    }

    /// <summary>
    /// A model for paging query.(include custom data)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginationFilter<T> : PaginationFilter
    {
        public PaginationFilter(int pageIndex, int pageNum, T data) : base(pageIndex, pageNum)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
