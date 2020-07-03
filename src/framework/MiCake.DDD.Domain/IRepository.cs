using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Defined a DDD repository interface.Please use <see cref="IRepository{TAggregateRoot, TKey}"/>.
    /// </summary>
    public interface IRepository
    {
    }

    /// <summary>
    /// A common interface is given to implement aggregateroot operations
    /// </summary>
    /// <typeparam name="TAggregateRoot"><see cref="IAggregateRoot"/></typeparam>
    /// <typeparam name="TKey">Primary key of aggregateroot</typeparam>
    public interface IRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        void Add(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Add a new aggregateRoot.
        /// </summary>
        Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.sometimes can use this way to get primary key.
        /// 
        /// <para>
        /// For some types whose ID is self increasing, the result can be obtained only after the database operation is performed.
        /// So,you need keep <paramref name="autoExecute"/> true.
        /// </para>
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to be added.</param>
        /// <param name="autoExecute">Auto execute save method(sql).Default value:true</param>
        TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute = true);

        /// <summary>
        /// Add a new aggregateRoot.and return this aggregate.sometimes can use this way to get primary key.
        /// 
        /// <para>
        /// For some types whose ID is self increasing, the result can be obtained only after the database operation is performed.
        /// So,you need keep <paramref name="autoExecute"/> true.
        /// </para>
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to be added.</param>
        /// <param name="autoExecute">Auto execute save method(sql).Default value:true</param>
        /// <param name="cancellationToken"></param>
        Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        void Update(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        void Delete(TAggregateRoot aggregateRoot);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
    }
}
