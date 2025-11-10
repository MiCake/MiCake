using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// Create a <see cref="IRepository"/> instance based on the <see cref="IRepositoryProvider{TAggregateRoot, TKey}"/>
    /// </summary>
    public interface IRepositoryFactory<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TKey : notnull
    {
        /// <summary>
        /// Create a <see cref="IRepository{TAggregateRoot, TKey}"/> instance.
        /// </summary>
        IRepository<TAggregateRoot, TKey> CreateRepository();

        /// <summary>
        /// Create a <see cref="IReadOnlyRepository{TAggregateRoot, TKey}"/> instance.
        /// </summary>
        IReadOnlyRepository<TAggregateRoot, TKey> CreateReadOnlyRepository();
    }
}
