using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// Provider a way to get <see cref="IFreeRepository"/>
    /// </summary>
    public interface IFreeRepositoryProvider<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// Get a instance of <see cref="IRepository{TAggregateRoot, TKey}"/>
        /// </summary>
        /// <returns></returns>
        IFreeRepository<TEntity, TKey> GetFreeRepository();

        /// <summary>
        /// Get a instance of <see cref="IReadOnlyRepository{TAggregateRoot, TKey}"/>
        /// </summary>
        /// <returns></returns>
        IReadOnlyFreeRepository<TEntity, TKey> GetReadOnlyFreeRepository();
    }
}
