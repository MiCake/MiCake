using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// A Repository only has get method
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey">The key type of <typeparamref name="TAggregateRoot"/></typeparam>
    public interface IReadOnlyRepository<TAggregateRoot, TKey> : IRepository
        where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Find your AggrageteRoot with primary key
        /// </summary>
        /// <param name="ID">Primary key of the aggrageteRoot to get</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of all aggrageteroot.
        /// </summary>
        Task<long> GetCountAsync(CancellationToken cancellationToken = default);
    }
}
