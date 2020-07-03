using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.Freedom
{
    public interface IReadOnlyFreeRepository<TEntity, TKey> : IFreeRepository
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// Get all entities.
        /// </summary>
        /// <returns>Entity</returns>
        IQueryable<TEntity> GetAll();

        /// <summary>
        /// Get all entities.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>Entity</returns>
        Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count of all entities.
        /// </summary>
        long GetCount();

        /// <summary>
        /// Get total count of all entities.
        /// </summary>
        Task<long> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Find entity with primary key
        /// </summary>
        /// <param name="ID">Primary key of the aggrageteRoot to get</param>
        TEntity Find(TKey ID);

        /// <summary>
        /// Find entity with primary key
        /// </summary>
        /// <param name="ID">>Primary key of the aggrageteRoot to get</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get entity collection according to filter criteria
        /// </summary>
        /// <param name="propertySelectors">filter expression</param>
        /// <returns></returns>
        IQueryable<TEntity> FindMatch(Expression<Func<TEntity, bool>> propertySelectors);

        /// <summary>
        /// Get entity collection according to filter criteria asynchronous.
        /// </summary>
        /// <param name="propertySelectors">filter expression</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default);
    }
}
