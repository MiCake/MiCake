using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Read-only repository implementation for Entity Framework Core.
    /// Provides query and retrieval operations for aggregate roots without modification capabilities.
    /// This is useful for read-only scenarios like reporting, queries, and data display.
    /// </summary>
    public class EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the read-only repository.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFReadOnlyRepository(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public virtual IQueryable<TAggregateRoot> Query()
        {
            return Entities;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual async Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.LongCountAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<TAggregateRoot?> FindAsync(TKey id, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> includeFunc, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable();

            if (includeFunc != null)
            {
                query = includeFunc(query);
            }

            return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
        }
    }
}
