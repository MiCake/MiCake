using MiCake.DDD.Domain;
using MiCake.Util.LinqFilter;
using MiCake.Util.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// EF Core repository implementation with paging query support
    /// </summary>
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<TDbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
            where TKey : notnull
    {
        private readonly Sort _defaultSort = new() { PropertyName = "Id", Ascending = false };

        /// <summary>
        /// Creates a new repository instance with paging support
        /// </summary>
        public EFRepositoryHasPaging(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Paging query data from repository by <see cref="PagingRequest"/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync(PagingRequest queryModel, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var result = await dbset.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Paging query data from repository and specify a sort selector
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingRequest queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);

            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await dbset.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await dbset.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Using <see cref="FilterGroup"/> to paging query data from repository
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest queryModel, FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Using <see cref="CompositeFilterGroup"/> to paging query data from repository
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest queryModel, CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Using <see cref="FilterGroup"/> to query data from repository
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Using <see cref="CompositeFilterGroup"/> to query data from repository
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
