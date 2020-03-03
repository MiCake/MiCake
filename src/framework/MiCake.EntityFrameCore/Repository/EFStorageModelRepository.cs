using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFStorageModelRepository<TDbContext, TAggregateRoot, TStorageModel, TKey> :
        EFStorageModelReadOnlyRepository<TDbContext, TAggregateRoot, TStorageModel, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasStorageModel
        where TStorageModel : class, IStorageModel
        where TDbContext : DbContext
    {
        public EFStorageModelRepository(IUnitOfWorkManager uowManager) : base(uowManager)
        {
        }

        public void Add(TAggregateRoot aggregateRoot)
        {
            DbContext.Add(ToStorageModel(aggregateRoot));
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
        {
            var addSnapshotEntity = DbContext.Add(ToStorageModel(aggregateRoot)).Entity;
            return ToEntity(addSnapshotEntity);
        }

        public async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var addSnapshotEntity = await DbContext.AddAsync(ToStorageModel(aggregateRoot), cancellationToken);
            return ToEntity(addSnapshotEntity.Entity);
        }

        public async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbContext.AddAsync(ToStorageModel(aggregateRoot), cancellationToken);
        }

        public void Delete(TAggregateRoot aggregateRoot)
        {
            DbSet.Remove(ToStorageModel(aggregateRoot));
        }

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(ToStorageModel(aggregateRoot));
            return Task.CompletedTask;
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            DbSet.Update(ToStorageModel(aggregateRoot));
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Update(ToStorageModel(aggregateRoot));
            return Task.CompletedTask;
        }
    }
}
