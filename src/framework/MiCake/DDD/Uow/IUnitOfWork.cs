using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Unit of Work interface for managing database transactions.
    /// When created, the UoW is immediately active. Transactions are automatically started if configured.
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
        /// Indicates if this unit of work has been completed (committed or marked as completed)
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Indicates if transactions are currently active
        /// </summary>
        bool HasActiveTransactions { get; }

        /// <summary>
        /// Gets the transaction isolation level for this unit of work
        /// </summary>
        IsolationLevel? IsolationLevel { get; }

        /// <summary>
        /// Gets the parent unit of work if this is a nested UoW
        /// </summary>
        IUnitOfWork? Parent { get; }

        /// <summary>
        /// Indicates if this is a nested unit of work
        /// </summary>
        bool IsNested => Parent != null;

        /// <summary>
        /// Commits all changes to the database.
        /// For nested UoW, this only marks as completed; actual commit happens at root level.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes.
        /// For nested UoW, this marks parent UoW to rollback.
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
