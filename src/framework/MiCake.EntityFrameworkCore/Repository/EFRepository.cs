using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// EF Core repository implementation for aggregate roots
    /// </summary>
    public class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Creates a new repository instance
        /// </summary>
        public EFRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Add a new aggregate root and return it
        /// </summary>
        public virtual async Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool saveNow = true, CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entityInfo = await dbcontext.Set<TAggregateRoot>().AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);

            if (saveNow)
                await dbcontext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entityInfo.Entity;
        }

        /// <summary>
        /// Add a new aggregate root
        /// </summary>
        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clear change tracking for the repository
        /// </summary>
        public async Task ClearChangeTrackingAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbcontext.ChangeTracker.Clear();
        }

        /// <summary>
        /// Delete an aggregate root
        /// </summary>
        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Remove(aggregateRoot);
        }

        /// <summary>
        /// Delete an aggregate root by its ID
        /// </summary>
        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.Where(e => e.Id.Equals(id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Save changes to the database
        /// </summary>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await dbcontext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Update an aggregate root
        /// </summary>
        public virtual async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Update(aggregateRoot);
        }
    }
}
