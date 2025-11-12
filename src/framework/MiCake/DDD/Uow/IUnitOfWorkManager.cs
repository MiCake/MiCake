using System;

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
        /// Begins a new Unit of Work with default options.
        /// If a UoW already exists and requiresNew is false, returns a nested UoW.
        /// The UoW is immediately active and transactions are started based on options.
        /// </summary>
        /// <param name="requiresNew">Whether to create a new root UoW even if one already exists</param>
        /// <returns>The Unit of Work instance</returns>
        IUnitOfWork Begin(bool requiresNew = false);

        /// <summary>
        /// Begins a new Unit of Work with custom options.
        /// If a UoW already exists and requiresNew is false, returns a nested UoW (options are inherited from parent).
        /// The UoW is immediately active and transactions are started based on options.
        /// </summary>
        /// <param name="options">Configuration options for the unit of work</param>
        /// <param name="requiresNew">Whether to create a new root UoW even if one already exists</param>
        /// <returns>The Unit of Work instance</returns>
        IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false);
    }
}
