using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public EFRepository(IDbContextProvider<TDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public void Add(TAggregateRoot aggregateRoot)
            => DbContext.Add(aggregateRoot);

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
            => DbContext.Add(aggregateRoot).Entity;

        public async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var entityInfo = await DbContext.AddAsync(aggregateRoot, cancellationToken);
            return entityInfo.Entity;
        }

        public async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => await DbContext.AddAsync(aggregateRoot, cancellationToken);

        public void Delete(TAggregateRoot aggregateRoot)
            => DbContext.Remove(aggregateRoot);

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbContext.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public void Update(TAggregateRoot aggregateRoot)
            => DbContext.Update(aggregateRoot);

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbContext.Update(aggregateRoot);
            return Task.CompletedTask;
        }
    }
}
