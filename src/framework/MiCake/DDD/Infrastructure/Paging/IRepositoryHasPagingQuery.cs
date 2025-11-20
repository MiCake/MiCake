using MiCake.Util.LinqFilter;
using MiCake.Util.Paging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain   // still using MiCake.DDD.Domain for compatibility
{
    /// <summary>
    /// A repository has paging query.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRepositoryHasPagingQuery<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey> where TAggregateRoot : class, IAggregateRoot<TKey> where TKey : notnull
    {
        /// <summary>
        /// Paing query data from repository by <see cref="PagingRequest"/>
        /// </summary>
        /// <param name="pagingRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingResponse<TAggregateRoot>> PagingQueryAsync(PagingRequest pagingRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paing query data from repository and specify a sort selector.
        /// </summary>
        /// <typeparam name="TOrderKey"></typeparam>
        /// <param name="pagingRequest"></param>
        /// <param name="orderSelector"></param>
        /// <param name="asc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingResponse<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingRequest pagingRequest, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Using <see cref="FilterGroup"/> to query data from repository.
        /// </summary>
        Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Using <see cref="CompositeFilterGroup"/> to query data from repository.
        /// </summary>
        Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Using <see cref="FilterGroup"/> to paging query data from repository.
        /// </summary>
        Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest pagingRequest, FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Using <see cref="CompositeFilterGroup"/> to paging query data from repository.
        /// </summary>
        Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest pagingRequest, CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default);
    }
}
