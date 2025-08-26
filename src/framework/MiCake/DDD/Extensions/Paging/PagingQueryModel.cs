using System;

namespace MiCake.DDD.Extensions.Paging
{
    /// <summary>
    /// A model for paging query.
    /// </summary>
    public class PagingQueryModel
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public PagingQueryModel(int pageIndex, int pageSize)
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
    public class PagingQueryModel<T>(int pageIndex, int pageSize, T data) : PagingQueryModel(pageIndex, pageSize)
    {
        public T Data { get; set; } = data;
    }
}
