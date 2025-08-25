using MiCake.Core.Util.LinqFilter;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<TDbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
    {
        private readonly Sort defaultSort = new() { PropertyName = "Id", Ascending = false };

        public EFRepositoryHasPaging(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync(PagingQueryModel queryModel, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var result = await dbset.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        public async Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingQueryModel queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);

            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await dbset.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            }
            else
            {
                result = await dbset.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            }
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        public async Task<PagingQueryResult<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingQueryModel queryModel, FilterGroup filterGroup, List<Sort> sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            var count = await query.CountAsync(cancellationToken);

            return new PagingQueryResult<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        public async Task<PagingQueryResult<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingQueryModel queryModel, CompositeFilterGroup compositeFilterGroup, List<Sort> sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            var count = await query.CountAsync(cancellationToken);

            return new PagingQueryResult<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, List<Sort> sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [defaultSort]);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort> sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [defaultSort]);

            return await query.ToListAsync(cancellationToken);
        }
    }
}
