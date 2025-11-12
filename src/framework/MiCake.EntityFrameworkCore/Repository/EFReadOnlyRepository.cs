using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// A read-only EF Core Repository base class.
    /// </summary>
    public class EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey> :
        EFRepositoryBase<TDbContext, TAggregateRoot, TKey>,
        IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
        where TKey : notnull
    {
        /// <summary>
        /// Creates a new read-only repository instance
        /// </summary>
        public EFReadOnlyRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Returns an IQueryable for complex queries
        /// </summary>
        public virtual IQueryable<TAggregateRoot> Query()
        {
            return Entities;
        }

        /// <summary>
        /// Find an aggregate root by its primary key
        /// </summary>
        public virtual async Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets total count of all aggregate roots
        /// </summary>
        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            return await dbset.LongCountAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
