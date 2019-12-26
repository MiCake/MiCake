using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.Freedom
{
    public interface IReadOnlyFreeRepository<TEntity, TKey> : IFreeRepository
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// Gets a list of all the entities.
        /// </summary>
        /// <param name="includeDetails">Set true to include all children of this entity</param>
        /// <returns>Entity</returns>
        List<TEntity> GetList();

        /// <summary>
        /// Gets a list of all the entities.
        /// </summary>
        /// <param name="includeDetails">Set true to include all children of this entity</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>Entity</returns>
        Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of all entities.
        /// </summary>
        long GetCount();

        /// <summary>
        /// Gets total count of all entities.
        /// </summary>
        Task<long> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Find your AggrageteRoot with primary key
        /// </summary>
        /// <param name="ID">Primary key of the aggrageteRoot to get</param>
        TEntity Find(TKey ID);

        /// <summary>
        /// Find your AggrageteRoot with primary key
        /// </summary>
        /// <param name="ID">>Primary key of the aggrageteRoot to get</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertySelectors"></param>
        /// <returns></returns>
        IQueryable<TEntity> FindMatch(params Expression<Func<TEntity, object>>[] propertySelectors);
    }
}
