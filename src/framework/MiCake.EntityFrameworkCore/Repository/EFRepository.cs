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
        public virtual async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            ThrowIfReadOnlyUnitOfWork();

            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the aggregate and flushes the current unit of work so a database-generated
        /// identity is populated on the instance, then returns the generated key.
        /// The flush happens inside the ambient writable unit of work transaction and does
        /// not commit: the write is durable only when that unit of work commits. The flush
        /// also persists every other pending change tracked by the current unit of work.
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to add</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The database-generated identity of the added aggregate</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no active unit of work is found, or when the unit of work is read-only.
        /// </exception>
        public virtual async Task<TKey> AddAndGetIdAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            // Require the ambient writable UoW before touching the tracker so a missing UoW
            // cannot silently leave the aggregate added to a bypass context's ChangeTracker.
            var current = Dependencies.UnitOfWorkManager.Current
                ?? throw new InvalidOperationException(
                    $"AddAndGetIdAsync on {typeof(TAggregateRoot).Name} requires an active writable unit of work. " +
                    "Begin one with IUnitOfWorkManager.BeginAsync() before adding.");

            await AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
            await current.FlushAsync(cancellationToken).ConfigureAwait(false);

            return aggregateRoot.Id;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            ThrowIfReadOnlyUnitOfWork();

            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Remove(aggregateRoot);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            ThrowIfReadOnlyUnitOfWork();

            // Loads the aggregate into the stable UoW context and then performs tracked
            // deletion so audit, soft deletion, domain events, and rollback semantics match
            // DeleteAsync. No operation when no aggregate with the id exists.
            var aggregate = await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (aggregate != null)
            {
                await DeleteAsync(aggregate, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            ThrowIfReadOnlyUnitOfWork();

            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Update(aggregateRoot);
        }

        /// <summary>
        /// Rejects a repository write when the ambient unit of work is read-only, so the
        /// mutation fails immediately instead of silently modifying the ChangeTracker of a
        /// read-only unit of work whose completion would discard the change. Missing units
        /// of work (Permissive access) are not affected and remain unguarded here.
        /// </summary>
        private void ThrowIfReadOnlyUnitOfWork()
        {
            var current = Dependencies.UnitOfWorkManager.Current;
            if (current?.IsReadOnly == true)
            {
                throw new InvalidOperationException(
                    $"Repository write on {typeof(TAggregateRoot).Name} is rejected: the active unit of work {current.Id} is read-only.");
            }
        }
    }
}
