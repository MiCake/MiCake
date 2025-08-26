using System;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Clean Unit of Work Manager interface
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable
    {
        /// <summary>
        /// Gets the current Unit of Work instance
        /// </summary>
        IUnitOfWork Current { get; }

        /// <summary>
        /// Begins a new Unit of Work
        /// </summary>
        /// <param name="requiresNew">Whether to create a new UoW even if one already exists</param>
        /// <returns>The Unit of Work instance</returns>
        IUnitOfWork Begin(bool requiresNew = false);
    }
}
