namespace MiCake.Uow
{
    /// <summary>
    /// Define a unit of work.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// The ID of this unit of work.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// Indicate unit of work is disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// The option of unit of work.<see cref="UnitOfWorkOptions"/>
        /// </summary>
        UnitOfWorkOptions UnitOfWorkOptions { get; }

        /// <summary>
        /// The interceptors for current uow.
        /// </summary>
        List<IUnitOfWorkInterceptor> Interceptors { get; }

        /// <summary>
        /// The <see cref="IServiceProvider"/> the unit of work depends on.
        /// The services created through this ServicScope are released together with the unit of work.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Commits all changes made to the database in the current unit of work asynchronously.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discards all changes made to the database in the current unit of work asynchronously.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current unit of work includes all transaction objects.
        /// </summary>
        /// <returns></returns>
        List<ITransactionObject> GetTransactionObjects();

    }
}
