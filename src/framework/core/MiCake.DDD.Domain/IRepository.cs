namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Defined a DDD repository interface.Please use <see cref="IRepository{TAggregateRoot, TKey}"/>.
    /// </summary>
    public interface IRepository
    {
    }


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
        ValueTask<TAggregateRoot?> FindAsync(TKey ID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total count of all aggrageteroot.
        /// </summary>
        ValueTask<long> GetCountAsync(CancellationToken cancellationToken = default);
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
        Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new aggregateRoot and return this aggregate.sometimes can use this way to get primary key.
        /// 
        /// <para>
        /// For some types whose ID is self increasing, the result can be obtained only after the database operation is performed.
        /// So,you need keep <paramref name="autoExecute"/> true.
        /// </para>
        /// </summary>
        /// <param name="aggregateRoot">The aggregate root to be added.</param>
        /// <param name="autoExecute">Auto execute save method(sql).Default value:true</param>
        /// <param name="cancellationToken"></param>
        ValueTask<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update aggregateRoot.
        /// </summary>
        Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot from repository
        /// </summary>
        Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete aggregateRoot form repository by id.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteByIdAsync(TKey ID, CancellationToken cancellationToken = default);
    }
}
