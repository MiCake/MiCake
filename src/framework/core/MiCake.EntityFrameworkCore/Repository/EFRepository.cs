using MiCake.Core.Util;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Repository
{
    public abstract class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public EFRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public virtual async ValueTask<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = false, CancellationToken cancellationToken = default)
        {
            var entityInfo = await DbSet.AddAsync(aggregateRoot, cancellationToken);

            if (autoExecute)
                await CurrentDbContext!.SaveChangesAsync(cancellationToken);

            return entityInfo.Entity;
        }

        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(aggregateRoot, cancellationToken);
        }

        public virtual Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public virtual async Task DeleteByIdAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(ID, nameof(ID));

            var item = await DbSet.FindAsync(new object[] { ID! }, cancellationToken);
            if (item != null)
                DbSet.Remove(item);
        }

        public virtual Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            DbSet.Update(aggregateRoot);
            return Task.CompletedTask;
        }
    }
}
