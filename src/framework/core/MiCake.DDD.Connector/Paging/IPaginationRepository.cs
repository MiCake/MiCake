using MiCake.DDD.Domain;
using System.Linq.Expressions;

namespace MiCake.Cord.Paging
{
    /// <summary>
    /// A repository has pagination.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IPaginationRepository<TAggregateRoot, TKey> where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Paing query data from repository by <see cref="PaginationFilter"/>
        /// </summary>
        /// <param name="queryModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync(PaginationFilter queryModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paing query data from repository and specify a sort selector.
        /// </summary>
        /// <typeparam name="TOrderKey"></typeparam>
        /// <param name="queryModel"></param>
        /// <param name="orderSelector"></param>
        /// <param name="asc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagingQueryResult<IEnumerable<TAggregateRoot>>> PagingQueryAsync<TOrderKey>(PaginationFilter queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default);
    }
}
