using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Unit of Work Manager interface.
    /// Manages the lifecycle of Unit of Work instances and supports nested transactions.
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable
    {
        /// <summary>
        /// Gets the current Unit of Work instance (may be null if no UoW is active)
        /// </summary>
        IUnitOfWork? Current { get; }

        /// <summary>
        /// Begins a new Unit of Work asynchronously with default options.
        /// If a UoW already exists and requiresNew is false, returns a nested UoW.
        /// </summary>
        /// <param name="requiresNew">Whether to create a new root UoW even if one already exists</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The Unit of Work instance</returns>
        Task<IUnitOfWork> BeginAsync(bool requiresNew = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new Unit of Work asynchronously with custom options.
        /// If a UoW already exists and requiresNew is false, returns a nested UoW (options are inherited from parent).
        /// </summary>
        /// <param name="options">Configuration options for the unit of work</param>
        /// <param name="requiresNew">Whether to create a new root UoW even if one already exists</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The Unit of Work instance</returns>
        Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, bool requiresNew = false, CancellationToken cancellationToken = default);
    }
}
