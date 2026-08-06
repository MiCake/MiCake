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
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            await dbset.AddAsync(aggregateRoot, cancellationToken).ConfigureAwait(false);
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
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            dbset.Update(aggregateRoot);
        }
    }
}
