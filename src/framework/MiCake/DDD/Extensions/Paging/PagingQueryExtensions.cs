using System;
using System.Linq;
using MiCake.DDD.Extensions.Paging;

namespace MiCake.Core.Util.LinqFilter;

public static class PagingQueryExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, PagingQueryModel queryModel)
    {
        if (queryModel == null)
        {
            throw new ArgumentNullException(nameof(queryModel), "PagingQueryModel cannot be null.");
        }

        return query.Page(queryModel.PageIndex, queryModel.PageSize);
    }
}
