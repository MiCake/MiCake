using MiCake.Cord.Paging;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MiCake.EntityFrameworkCore.Repository
{
    public abstract class EFPaginationRepository<TDbContext, TAggregateRoot, TKey> : EFRepository<DbContext, TAggregateRoot, TKey>, IPaginationRepository<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
    {
        public EFPaginationRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        public async Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PaginationFilter queryModel, CancellationToken cancellationToken = default)
        {
            var result = await DbSet.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageNumber, count, result);
        }

        public async Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync<TOrderKey>(PaginationFilter queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await DbSet.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            }
            else
            {
                result = await DbSet.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken);
            }
            var count = await GetCountAsync(cancellationToken);

            return new PagingQueryResult<IEnumerable<TAggregateRoot>>(queryModel.PageNumber, count, result);
        }
    }
}
