using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
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
        /// The <see cref="IServiceScope"/> the unit of work depends on.
        /// The services created through this ServicScope are released together with the unit of work.
        /// </summary>
        IServiceScope ServiceScope { get; }

        /// <summary>
        /// Try add a <see cref="IDbExecutor"/> to current <see cref="IUnitOfWork"/> asynchronously. 
        /// Unit of work transactions will be given to the current <see cref="IDbExecutor"/>.
        /// </summary>
        /// <param name="dbExecutor">Expected to be added <see cref="IDbExecutor"/></param>
        /// <param name="cancellationToken"></param>
        Task<bool> TryAddDbExecutorAsync(IDbExecutor dbExecutor, CancellationToken cancellationToken = default);

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
    }
}
