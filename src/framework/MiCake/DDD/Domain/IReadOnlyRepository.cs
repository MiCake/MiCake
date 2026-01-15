using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// A common interface of read-only repository operations
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey">The key type of <typeparamref name="TAggregateRoot"/></typeparam>
    public interface IReadOnlyRepository<TAggregateRoot, TKey> : IRepository
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TKey : notnull
    {
        /// <summary>
        /// Returns an IQueryable for complex queries.
        /// This allows users to build complex LINQ queries against the aggregate root.
        /// </summary>
        /// <returns>IQueryable of the aggregate root</returns>
        IQueryable<TAggregateRoot> Query();

        /// <summary>
        /// Find your AggregateRoot with primary key
        /// </summary>
        /// <param name="id">Primary key of the aggregateRoot to get</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Find your AggregateRoot with primary key, include navigation properties by includeFunc
        /// </summary>
        /// <param name="id">Primary key of the aggregateRoot to get</param>
        /// <param name="includeFunc">Function to include navigation properties</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<TAggregateRoot?> FindAsync(TKey id, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> includeFunc , CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of all aggregateroot.
        /// </summary>
        Task<long> GetCountAsync(CancellationToken cancellationToken = default);
    }
}
