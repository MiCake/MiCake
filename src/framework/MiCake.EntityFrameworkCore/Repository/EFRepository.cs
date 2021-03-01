using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
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
        public EFRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public virtual void Add(TAggregateRoot aggregateRoot)
            => DbContext.Add(aggregateRoot);

        public virtual TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute = false)
        {
            var entity = DbContext.Add(aggregateRoot);

            if (autoExecute)
            {
                DbContext.SaveChanges();
            }

            return entity.Entity;
        }
        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = false, CancellationToken cancellationToken = default)
        {
            var entityInfo = await DbContext.AddAsync(aggregateRoot, cancellationToken);

            if (autoExecute)
                await DbContext.SaveChangesAsync(cancellationToken);

            return entityInfo.Entity;
        }

        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => await DbContext.AddAsync(aggregateRoot, cancellationToken);

        public virtual void Delete(TAggregateRoot aggregateRoot)
            => DbContext.Remove(aggregateRoot);

        public virtual Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbContext.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public virtual async Task DeleteByIdAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var item = await DbSet.FindAsync(new object[] { ID }, cancellationToken);
            if (item != null)
                DbSet.Remove(item);
        }

        public virtual void Update(TAggregateRoot aggregateRoot)
            => DbContext.Update(aggregateRoot);

        public virtual Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbContext.Update(aggregateRoot);
            return Task.CompletedTask;
        }
    }
}
