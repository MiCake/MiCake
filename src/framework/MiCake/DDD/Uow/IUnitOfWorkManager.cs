using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Unit of Work Manager interface.
    /// Manages ambient unit of work frames and supports shared nested units of work, strict
    /// requiresNew isolated execution, and context-agnostic isolated execution.
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the current (ambient) Unit of Work instance, or null when no UoW is active.
        /// When null, prefer <see cref="ExecuteIsolatedAsync(Func{IServiceProvider, CancellationToken, Task}, UnitOfWorkOptions, CancellationToken)"/>
        /// for isolated write blocks; the strict entries require an ambient UoW to be established first.
        /// </summary>
        IUnitOfWork? Current { get; }

        /// <summary>
        /// Begins a Unit of Work with the given options. If a live UoW already exists, returns a
        /// shared nested UoW that inherits its read-only intent and isolation level and delegates
        /// all physical work to the root. The caller owns disposal of the returned instance.
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
        /// This strict precondition doubles as a wiring self-check; use
        /// <see cref="ExecuteIsolatedAsync(Func{IServiceProvider, CancellationToken, Task}, UnitOfWorkOptions, CancellationToken)"/>
        /// when the same call site must also run without an ambient UoW.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <seealso cref="IStandaloneUnitOfWorkExecutor"/>
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
        /// This strict precondition doubles as a wiring self-check; use
        /// <see cref="ExecuteIsolatedAsync{TResult}(Func{IServiceProvider, CancellationToken, Task{TResult}}, UnitOfWorkOptions, CancellationToken)"/>
        /// when the same call site must also run without an ambient UoW.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        /// <seealso cref="IStandaloneUnitOfWorkExecutor"/>
        Task<TResult> ExecuteRequiresNewAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="operation"/> in a fully isolated DI scope with its own root unit of work,
        /// regardless of any ambient unit of work. Commits on success and rolls back on failure.
        /// When an ambient UoW exists it is suspended and restored on every exit path (success, failure,
        /// cancellation, commit failure, rollback failure); when none exists, restoration is a no-op.
        /// The callback must resolve repositories and DbContexts from the supplied provider.
        /// Recommended default entry for isolated write blocks in any host.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <seealso cref="IStandaloneUnitOfWorkExecutor"/>
        /// <seealso cref="ExecuteRequiresNewAsync(Func{IServiceProvider, CancellationToken, Task}, UnitOfWorkOptions, CancellationToken)"/>
        Task ExecuteIsolatedAsync(
            Func<IServiceProvider, CancellationToken, Task> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="operation"/> in a fully isolated DI scope with its own root unit of work,
        /// regardless of any ambient unit of work. Commits on success and rolls back on failure.
        /// When an ambient UoW exists it is suspended and restored on every exit path (success, failure,
        /// cancellation, commit failure, rollback failure); when none exists, restoration is a no-op.
        /// The callback must resolve repositories and DbContexts from the supplied provider.
        /// Recommended default entry for isolated write blocks in any host.
        /// </summary>
        /// <param name="operation">The operation to execute inside the isolated scope</param>
        /// <param name="options">Options for the inner unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        /// <seealso cref="IStandaloneUnitOfWorkExecutor"/>
        /// <seealso cref="ExecuteRequiresNewAsync(Func{IServiceProvider, CancellationToken, Task}, UnitOfWorkOptions, CancellationToken)"/>
        Task<TResult> ExecuteIsolatedAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
