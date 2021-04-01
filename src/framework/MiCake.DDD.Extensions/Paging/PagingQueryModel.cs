using System;

namespace MiCake.DDD.Extensions.Paging
{
    /// <summary>
    /// A model for paging query.
    /// </summary>
    public class PagingQueryModel
    {
        public int PageIndex { get; set; }

        public int PageNum { get; set; }

        public PagingQueryModel(int pageIndex, int pageNum)
        {
            if (pageIndex < 0 || pageNum < 0)
                throw new ArgumentException("page index and page num cannot less than zero.");

            PageIndex = pageIndex;
            PageNum = pageNum;
        }

        public int CurrentStartNo => (PageIndex - 1) * PageNum;

    }

    /// <summary>
    /// A model for paging query.(include custom data)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagingQueryModel<T> : PagingQueryModel
    {
        public PagingQueryModel(int pageIndex, int pageNum, T data) : base(pageIndex, pageNum)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
