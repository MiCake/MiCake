using System;
using System.Linq;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;

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
}
