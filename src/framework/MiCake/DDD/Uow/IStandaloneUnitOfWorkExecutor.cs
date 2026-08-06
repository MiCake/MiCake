using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Executes an application operation in a standalone unit of work without an ambient UoW.
    /// Rejects an existing ambient UoW. The executor owns the isolated DI scope, the UoW,
    /// the commit, the rollback, and the asynchronous disposal lifecycle.
    /// </summary>
    public interface IStandaloneUnitOfWorkExecutor
    {
        /// <summary>
        /// Executes <paramref name="operation"/> in a new isolated scope with a root writable unit of work.
        /// Commits on success and rolls back on failure. The original exception is preserved unless
        /// rollback also fails, in which case a <see cref="Exceptions.UnitOfWorkBoundaryException"/> with both causes is thrown.
        /// </summary>
        Task ExecuteAsync(
            Func<IServiceProvider, CancellationToken, Task> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes <paramref name="operation"/> in a new isolated scope with a root writable unit of work.
        /// Commits on success and rolls back on failure. The original exception is preserved unless
        /// rollback also fails, in which case a <see cref="Exceptions.UnitOfWorkBoundaryException"/> with both causes is thrown.
        /// </summary>
        Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
