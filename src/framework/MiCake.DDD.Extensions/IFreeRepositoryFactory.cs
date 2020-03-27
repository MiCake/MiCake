using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// Create a <see cref="IFreeRepository"/> instance based on the <see cref="IFreeRepositoryProvider{TAggregateRoot, TKey}"/>
    /// </summary>
    public interface IFreeRepositoryFactory<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// Create a <see cref="IFreeRepository{TEntity, TKey}"/> instance.
        /// </summary>
        IFreeRepository<TEntity, TKey> CreateFreeRepository();

        /// <summary>
        /// Create a <see cref="IReadOnlyFreeRepository{TEntity, TKey}"/> instance.
        /// </summary>
        IReadOnlyFreeRepository<TEntity, TKey> CreateReadOnlyFreeRepository();
    }
}
