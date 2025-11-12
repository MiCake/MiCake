using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Full-featured repository implementation for Entity Framework Core.
    /// Provides complete CRUD operations for aggregate roots including add, update, delete, and query operations.
    /// This is the primary repository implementation for DDD aggregate roots with EF Core persistence.
    /// </summary>
    public class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the repository.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFRepository(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task ClearChangeTrackingAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbcontext.ChangeTracker.Clear();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Remove(aggregateRoot);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.Where(e => e.Id.Equals(id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var dbcontext = await GetDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await dbcontext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Update(aggregateRoot);
        }
    }
}
