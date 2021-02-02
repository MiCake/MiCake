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
        where TPersistentObject : class, IPersistentObject<TKey, TAggregateRoot>
        where TDbContext : DbContext
    {
        public EFRepositoryWithPO(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public virtual void Add(TAggregateRoot aggregateRoot)
        {
            DbContext.Add(ReverseConvert(aggregateRoot));
        }

        public virtual TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute = true)
        {
            var addedEntity = DbContext.Add(ReverseConvert(aggregateRoot)).Entity;

            if (autoExecute)
            {
                DbContext.SaveChanges();
            }

            return Convert(addedEntity);
        }

        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = true, CancellationToken cancellationToken = default)
        {
            var addedEntity = (await DbContext.AddAsync(ReverseConvert(aggregateRoot), cancellationToken)).Entity;

            if (autoExecute)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return Convert(addedEntity);
        }

        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbContext.AddAsync(ReverseConvert(aggregateRoot), cancellationToken);
        }

        public virtual void Delete(TAggregateRoot aggregateRoot)
        {
            DbSet.Remove(ReverseConvert(aggregateRoot));
        }

        public virtual Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(ReverseConvert(aggregateRoot));
            return Task.CompletedTask;
        }

        public virtual void Update(TAggregateRoot aggregateRoot)
        {
           ReverseConvert(aggregateRoot);   // will call update automtic
        }

        public virtual Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            ReverseConvert(aggregateRoot);  // will call update automtic
            return Task.CompletedTask;
        }
    }
}
