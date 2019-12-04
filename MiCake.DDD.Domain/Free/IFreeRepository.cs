using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.Free
{
    /// <summary>
    /// Mark a freedom repository. You can release the constraint that you must use aggregateroot
    /// </summary>
    public interface IFreeRepository
    {
    }

    public interface IFreeRepository<TEntity, TKey> : IReadOnlyFreeRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// Add a new TEntity.
        /// </summary>
        void Add(TEntity entity);

        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.you can get it Primary key
        /// </summary>
        TEntity AddAndReturn(TEntity entity);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.you can get it Primary key
        /// </summary>
        TEntity AddAndReturnAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        void Update(TEntity entity);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        void Delete(TEntity entity);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        void DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    }
}
