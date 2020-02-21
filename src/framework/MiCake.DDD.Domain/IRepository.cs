using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// a ddd repository interface.Please use <see cref="IRepository<>"/>.
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
    {
        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        void Add(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.you can get it Primary key
        /// </summary>
        TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.you can get it Primary key
        /// </summary>
        Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        void Update(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        void Delete(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
    }
}
