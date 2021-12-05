using MiCake.DDD.Domain;

namespace MiCake.DDD.Connector
{
    /// <summary>
    /// Provider a way to get <see cref="IRepository"/>
    /// </summary>
    public interface IRepositoryProvider<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Get a instance of <see cref="IRepository{TAggregateRoot, TKey}"/>
        /// </summary>
        /// <returns></returns>
        IRepository<TAggregateRoot, TKey> GetRepository();

        /// <summary>
        /// Get a instance of <see cref="IReadOnlyRepository{TAggregateRoot, TKey}"/>
        /// </summary>
        /// <returns></returns>
        IReadOnlyRepository<TAggregateRoot, TKey> GetReadOnlyRepository();
    }
}
