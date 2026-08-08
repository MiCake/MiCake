using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// A common interface is given to implement aggregateroot operations
    /// </summary>
    public interface IRepository
    {
    }

    /// <summary>
    /// A common interface is given to implement aggregateroot operations
    /// </summary>
    /// <typeparam name="TAggregateRoot"><see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TKey">Primary key of aggregateroot</typeparam>
    public interface IRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TKey : notnull
    {
        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        /// <remarks>
        /// Only modifies the tracked state of the current unit of work; persistence is
        /// owned by the unit of work (flush or commit). For identity-generating keys,
        /// call <see cref="MiCake.DDD.Uow.IUnitOfWork.FlushAsync"/> afterwards to obtain
        /// the generated key on the aggregate instance.
        /// </remarks>
        Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new aggregateRoot and flushes the current unit of work so a
        /// database-generated identity is populated on the instance, then returns the
        /// generated key.
        /// </summary>
        /// <remarks>
        /// The flush happens inside the ambient writable unit of work transaction and does
        /// not commit: the write is durable only when that unit of work commits. The flush
        /// also persists every other pending change tracked by the current unit of work.
        /// Requires an active writable unit of work; otherwise an
        /// <see cref="System.InvalidOperationException"/> is thrown.
        /// </remarks>
        /// <param name="aggregateRoot">The aggregate root to add</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The database-generated identity of the added aggregate</returns>
        Task<TKey> AddAndGetIdAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        /// <remarks>
        /// When the instance is not tracked, this performs a full detached aggregate
        /// replacement: every property of the supplied instance is written. Configured
        /// concurrency tokens are preserved, so a stale replacement that conflicts with
        /// a concurrently saved version surfaces as <c>DbUpdateConcurrencyException</c>
        /// during the unit of work flush or commit instead of silently overwriting it.
        /// Load-and-modify is the preferred workflow for partial updates.
        /// </remarks>
        Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository (tracked lifecycle deletion).
        /// </summary>
        Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository by id.
        /// </summary>
        /// <remarks>
        /// Loads the aggregate into the stable unit of work context and performs tracked
        /// deletion, so soft deletion, audit, domain events, and unit of work rollback
        /// semantics apply exactly as with <see cref="DeleteAsync"/>. No operation when
        /// no aggregate with the given id exists.
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
    }
}
