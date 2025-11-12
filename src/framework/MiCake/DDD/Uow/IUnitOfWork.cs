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
        /// Marks this unit of work to skip commit operations for performance optimization.
        /// This is useful for read-only operations or when you don't need to persist changes.
        /// </summary>
        Task MarkAsCompletedAsync(CancellationToken cancellationToken = default);

        #region Savepoint Support

        /// <summary>
        /// Creates a savepoint within the current transaction.
        /// Allows partial rollback to this point without rolling back the entire transaction.
        /// </summary>
        /// <param name="name">Name of the savepoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The name of the created savepoint</returns>
        Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back to a specific savepoint, discarding changes made after that point.
        /// The savepoint remains valid and can be rolled back to again.
        /// </summary>
        /// <param name="name">Name of the savepoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a savepoint, freeing its resources.
        /// The savepoint cannot be used after being released.
        /// </summary>
        /// <param name="name">Name of the savepoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

        #endregion

        #region Transaction Event Hooks

        /// <summary>
        /// Event raised before committing the transaction.
        /// Useful for validation or preparing data before commit.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnCommitting;

        /// <summary>
        /// Event raised after successfully committing the transaction.
        /// Useful for cache clearing, notifications, or other post-commit actions.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnCommitted;

        /// <summary>
        /// Event raised before rolling back the transaction.
        /// Useful for logging or preparing for rollback.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;

        /// <summary>
        /// Event raised after successfully rolling back the transaction.
        /// Useful for cleanup or error handling.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;

        #endregion

        #region Legacy - To be removed

        /// <summary>
        /// [DEPRECATED] Register a DbContext wrapper with this unit of work.
        /// This method is deprecated and will be removed in a future version.
        /// Use IUnitOfWorkInternal.RegisterResource instead.
        /// </summary>
        [Obsolete("This method is deprecated. Use IUnitOfWorkInternal.RegisterResource instead. This will be removed in a future version.")]
        void RegisterDbContext(IDbContextWrapper wrapper);

        #endregion
    }
}
