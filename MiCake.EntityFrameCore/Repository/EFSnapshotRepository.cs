using MiCake.DDD.Domain;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFSnapshotRepository<TDbContext, TAggregateRoot, TSnapshot, TKey> :
        EFSnapshotReadOnlyRepository<TDbContext, TAggregateRoot, TSnapshot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IEntityHasSnapshot<TSnapshot>
        where TSnapshot : class
        where TDbContext : DbContext
    {
        public EFSnapshotRepository(IUnitOfWorkManager uowManager) : base(uowManager)
        {
        }

        public void Add(TAggregateRoot aggregateRoot)
        {
            DbContext.Add(aggregateRoot.GetSnapshot());
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
        {
            var addSnapshotEntity = DbContext.Add(aggregateRoot.GetSnapshot()).Entity;
            return CreateFromSnapshot(addSnapshotEntity);
        }

        public async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var addSnapshotEntity = await DbContext.AddAsync(aggregateRoot.GetSnapshot(), cancellationToken);
            return CreateFromSnapshot(addSnapshotEntity.Entity);
        }

        public async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbContext.AddAsync(aggregateRoot.GetSnapshot(), cancellationToken);
        }

        public void Delete(TAggregateRoot aggregateRoot)
        {
            DbSet.Remove(aggregateRoot.GetSnapshot());
        }

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(aggregateRoot.GetSnapshot());
            return Task.CompletedTask;
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            DbSet.Update(aggregateRoot.GetSnapshot());
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Update(aggregateRoot.GetSnapshot());
            return Task.CompletedTask;
        }
    }
}
