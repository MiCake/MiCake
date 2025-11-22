using System;
using System.Linq;

namespace MiCake.Util.Query.Paging;

public static class PagingQueryExtensions
{
    /// <summary>
    /// Applies paging to an IQueryable based on the provided paging query model.
    /// </summary>
    /// <typeparam name="T">The type of elements in the query.</typeparam>
    /// <param name="query">The query to page.</param>
    /// <param name="queryModel">The paging query model containing page index and size.</param>
    /// <returns>The paged query.</returns>
    public static IQueryable<T> Page<T>(this IQueryable<T> query, PagingRequest queryModel)
    {
        if (queryModel == null)
        {
            throw new ArgumentNullException(nameof(queryModel), "PagingQueryModel cannot be null.");
        }

        return query.Page(queryModel.PageIndex, queryModel.PageSize);
    }

    /// <summary>
    /// Applies paging to an IQueryable based on the provided page number and page size.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int pageSize)
    {
        if (pageSize < 1)
        {
            throw new ArgumentException("Page size must be >= 1.", nameof(pageSize));
        }

        if (page < 1)
        {
            throw new ArgumentException("Page number must be >= 1.", nameof(page));
        }

        var pageIndex = page - 1;
        return query.Skip(pageIndex * pageSize).Take(pageSize);
    }
}
