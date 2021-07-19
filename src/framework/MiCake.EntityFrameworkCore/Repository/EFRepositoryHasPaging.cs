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
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<DbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
    {
        public EFRepositoryHasPaging(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        public async Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PagingQueryModel queryModel, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var result = await dbset.Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum).ToListAsync();
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageIndex, count, result);
        }

        public async Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync<TOrderKey>(PagingQueryModel queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);

            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await dbset.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum).ToListAsync();
            }
            else
            {
                result = await dbset.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum).ToListAsync();
            }
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageIndex, count, result);
        }
    }
}
