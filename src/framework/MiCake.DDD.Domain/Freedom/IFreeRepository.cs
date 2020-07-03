using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.Freedom
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
        /// Add a new entity.and return this entity.sometimes can use this way to get primary key.
        /// 
        /// <para>
        /// For some types whose ID is self increasing, the result can be obtained only after the database operation is performed.
        /// So,you need keep <paramref name="autoExecute"/> true.
        /// </para>
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        /// <param name="autoExecute">Auto execute save method(sql).Default value:true</param>
        TEntity AddAndReturn(TEntity entity, bool autoExecute = true);

        /// <summary>
        /// Add a new entity.and return this entity.sometimes can use this way to get primary key.
        /// 
        /// <para>
        /// For some types whose ID is self increasing, the result can be obtained only after the database operation is performed.
        /// So,you need keep <paramref name="autoExecute"/> true.
        /// </para>
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        /// <param name="autoExecute">Auto execute save method(sql).Default value:true</param>
        /// <param name="cancellationToken"></param>
        Task<TEntity> AddAndReturnAsync(TEntity entity, bool autoExecute = true, CancellationToken cancellationToken = default);

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
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
