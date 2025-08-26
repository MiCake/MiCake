using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Clean Unit of Work interface for managing database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Unique identifier for this unit of work
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Indicates if this unit of work is disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Indicates if this unit of work has been completed
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Indicates if transactions have been started for this unit of work
        /// </summary>
        bool HasActiveTransactions { get; }

        /// <summary>
        /// Begins transactions for all registered database contexts
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all changes to the database
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Register a DbContext wrapper with this unit of work (internal use)
        /// </summary>
        void RegisterDbContext(IDbContextWrapper wrapper);

        /// <summary>
        /// Marks this unit of work to skip commit operations for performance optimization.
        /// This is useful for read-only operations or when you don't need to persist changes.
        /// </summary>
        Task MarkAsCompletedAsync(CancellationToken cancellationToken = default);
    }
}
