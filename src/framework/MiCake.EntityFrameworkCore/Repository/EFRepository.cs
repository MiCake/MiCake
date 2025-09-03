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

        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = false, CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken);
            var entityInfo = await dbcontext.Set<TAggregateRoot>().AddAsync(aggregateRoot, cancellationToken);

            if (autoExecute)
                await dbcontext.SaveChangesAsync(cancellationToken);

            return entityInfo.Entity;
        }

        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            await dbset.AddAsync(aggregateRoot, cancellationToken);
        }

        public async Task ClearChangeTrackingAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken);
            dbcontext.ChangeTracker.Clear();
        }

        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            dbset.Remove(aggregateRoot);
        }

        public virtual async Task DeleteByIdAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            var item = await dbset.FindAsync(new object[] { ID }, cancellationToken);
            if (item != null)
                dbset.Remove(item);
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken);
            return await dbcontext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken);
            dbset.Update(aggregateRoot);
        }
    }
}
