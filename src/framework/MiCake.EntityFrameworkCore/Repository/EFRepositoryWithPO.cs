using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Use efcore as ORM and repository with persistent objects.
    /// </summary>
    /// <typeparam name="TDbContext">Type Of DBContext</typeparam>
    /// <typeparam name="TAggregateRoot">Type of <see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TPersistentObject">Type of <see cref="PersistentObject{TKey,TEntity, TPersistentObject}"/></typeparam>
    /// <typeparam name="TKey">Primary key type of <see cref="IAggregateRoot"/></typeparam>
    public class EFRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey> :
        EFReadOnlyRepositoryWithPO<TDbContext, TAggregateRoot, TPersistentObject, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>, IHasPersistentObject
        where TPersistentObject : class, IPersistentObject
        where TDbContext : DbContext
    {
        public EFRepositoryWithPO(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public virtual void Add(TAggregateRoot aggregateRoot)
        {
            DbContext.Add(POManager.MapToPO(aggregateRoot));
        }

        public virtual TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute = true)
        {
            var addedEntity = DbContext.Add(POManager.MapToPO(aggregateRoot)).Entity;

            if (autoExecute)
            {
                DbContext.SaveChanges();
            }

            return POManager.MapToDO(addedEntity);
        }

        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = true, CancellationToken cancellationToken = default)
        {
            var addedEntity = (await DbContext.AddAsync(POManager.MapToPO(aggregateRoot), cancellationToken)).Entity;

            if (autoExecute)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return POManager.MapToDO(addedEntity);
        }

        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbContext.AddAsync(POManager.MapToPO(aggregateRoot), cancellationToken);
        }

        public virtual void Delete(TAggregateRoot aggregateRoot)
        {
            DbSet.Remove(POManager.MapToPO(aggregateRoot));
        }

        public virtual Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(POManager.MapToPO(aggregateRoot));
            return Task.CompletedTask;
        }

        public virtual void Update(TAggregateRoot aggregateRoot)
        {
            DbSet.Update(POManager.MapToPO(aggregateRoot));
        }

        public virtual Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Update(POManager.MapToPO(aggregateRoot));
            return Task.CompletedTask;
        }
    }
}
