using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Use efcore as ORM and repository with persistent objects.
    /// </summary>
    /// <typeparam name="TDbContext">Type Of DBContext</typeparam>
    /// <typeparam name="TAggregateRoot">Type of <see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TPersistentObject">Type of <see cref="PersistentObject{TEntity}"/></typeparam>
    /// <typeparam name="TKey">Primary key type of <see cref="IAggregateRoot"/></typeparam>
    public class EFRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey> :
        EFReadOnlyRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject
        where TDbContext : DbContext
    {
        public EFRepositoryWithPO(IUnitOfWorkManager uowManager) : base(uowManager)
        {
        }

        public void Add(TAggregateRoot aggregateRoot)
        {
            DbContext.Add(ToPersistentObject(aggregateRoot));
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
        {
            var addSnapshotEntity = DbContext.Add(ToPersistentObject(aggregateRoot)).Entity;
            return ToEntity(addSnapshotEntity);
        }

        public async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var addSnapshotEntity = await DbContext.AddAsync(ToPersistentObject(aggregateRoot), cancellationToken);
            return ToEntity(addSnapshotEntity.Entity);
        }

        public async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbContext.AddAsync(ToPersistentObject(aggregateRoot), cancellationToken);
        }

        public void Delete(TAggregateRoot aggregateRoot)
        {
            DbSet.Remove(ToPersistentObject(aggregateRoot));
        }

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(ToPersistentObject(aggregateRoot));
            return Task.CompletedTask;
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            DbSet.Update(ToPersistentObject(aggregateRoot));
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Update(ToPersistentObject(aggregateRoot));
            return Task.CompletedTask;
        }
    }
}
