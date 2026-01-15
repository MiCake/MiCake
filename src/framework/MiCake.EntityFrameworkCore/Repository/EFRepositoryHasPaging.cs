using MiCake.DDD.Domain;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;
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
    /// Repository implementation with advanced paging and filtering capabilities for Entity Framework Core.
    /// Extends the full repository functionality with specialized methods for paginated queries and dynamic filtering.
    /// This is ideal for building list views, search functionality, and data grids with sorting and filtering.
    /// </summary>
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<TDbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
            where TKey : notnull
    {
        private readonly Sort _defaultSort = new() { PropertyName = "Id", Ascending = false };

        /// <summary>
        /// Initializes a new instance of the repository with paging support.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFRepositoryHasPaging(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync(PagingRequest pagingRequest, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var result = await dbset.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingRequest pagingRequest, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);

            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await dbset.OrderBy(orderSelector).Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await dbset.OrderByDescending(orderSelector).Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> FilterPagingQueryAsync(PagingRequest pagingRequest, FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> FilterPagingQueryAsync(PagingRequest pagingRequest, CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> FilterQueryAsync(FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> FilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
