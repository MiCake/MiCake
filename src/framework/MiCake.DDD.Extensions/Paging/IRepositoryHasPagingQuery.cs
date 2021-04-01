using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Paging
{
    /// <summary>
    /// A repository has paging query.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRepositoryHasPagingQuery<TAggregateRoot, TKey> where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PagingQueryModel queryModel, CancellationToken cancellationToken = default);

        Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PagingQueryModel queryModel, Func<TAggregateRoot, TKey> orderSelector, CancellationToken cancellationToken = default);
    }
}
