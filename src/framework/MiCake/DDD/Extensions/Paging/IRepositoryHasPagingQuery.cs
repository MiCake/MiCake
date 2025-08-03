using MiCake.Core.Util.LinqFilter;
using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Paging
{
    /// <summary>
    /// A repository has paging query.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRepositoryHasPagingQuery<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey> where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Paing query data from repository by <see cref="PagingQueryModel"/>
        /// </summary>
        /// <param name="queryModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync(PagingQueryModel queryModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paing query data from repository and specify a sort selector.
        /// </summary>
        /// <typeparam name="TOrderKey"></typeparam>
        /// <param name="queryModel"></param>
        /// <param name="orderSelector"></param>
        /// <param name="asc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingQueryModel queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default);

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
        Task<PagingQueryResult<TAggregateRoot>> CommonFilterPagingQueryAsync(
            PagingQueryModel queryModel,
            FilterGroup filterGroup,
            List<Sort>? sorts = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Using <see cref="CompositeFilterGroup"/> to paging query data from repository.
        /// </summary>
        Task<PagingQueryResult<TAggregateRoot>> CommonFilterPagingQueryAsync(
            PagingQueryModel queryModel,
            CompositeFilterGroup compositeFilterGroup,
            List<Sort>? sorts = null,
            CancellationToken cancellationToken = default);
    }
}
