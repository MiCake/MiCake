using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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


        public Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PagingQueryModel queryModel, CancellationToken cancellationToken = default)
        {
            var result = DbSet.Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum);

            return Task.FromResult(new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageIndex, GetCount(), result.ToList()));
        }

        public Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync<TOrderKey>(PagingQueryModel queryModel, Func<TAggregateRoot, TOrderKey> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = DbSet.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum);
            }
            else
            {
                result = DbSet.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageNum);
            }

            return Task.FromResult(new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageIndex, GetCount(), result.ToList()));
        }
    }
}
