using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Unit of Work Manager interface.
    /// Manages ambient unit of work frames and supports shared nested units of work and isolated requiresNew execution.
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the current Unit of Work instance (may be null if no UoW is active)
        /// </summary>
        IUnitOfWork? Current { get; }

        /// <summary>
        /// Begins a new Unit of Work asynchronously with default options.
        /// If a live UoW already exists, returns a shared nested UoW under it.
        /// </summary>
        /// <param name="options">Configuration options for the unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The Unit of Work instance</returns>
        Task<IUnitOfWork> BeginAsync(
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="operation"/> in a fully isolated DI scope with a new root writable UoW.
        /// Commits on success and rolls back on failure. The previous ambient frame is restored on success,
        /// failure, cancellation, commit failure, and rollback failure.
        /// Requires an active outer UoW; otherwise throws <see cref="InvalidOperationException"/>.
        /// The callback must resolve repositories and DbContexts from the supplied provider.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteRequiresNewAsync(
            Func<IServiceProvider, CancellationToken, Task> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="operation"/> in a fully isolated DI scope with a new root writable UoW.
        /// Commits on success and rolls back on failure. The previous ambient frame is restored on success,
        /// failure, cancellation, commit failure, and rollback failure.
        /// Requires an active outer UoW; otherwise throws <see cref="InvalidOperationException"/>.
        /// The callback must resolve repositories and DbContexts from the supplied provider.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        Task<TResult> ExecuteRequiresNewAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
