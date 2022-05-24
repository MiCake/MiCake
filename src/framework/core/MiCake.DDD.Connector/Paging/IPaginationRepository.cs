using MiCake.DDD.Domain;
using System.Linq.Expressions;

namespace MiCake.Cord.Paging
{
    /// <summary>
    /// A repository has pagination.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IPaginationRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey> where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Paing query data from repository by <see cref="PaginationFilter"/>
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync(PaginationFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paing query data from repository and specify a sort selector.
        /// </summary>
        /// <typeparam name="TOrderKey"></typeparam>
        /// <param name="filter"></param>
        /// <param name="orderSelector"></param>
        /// <param name="asc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PaginationFilter filter, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default);
    }
}
