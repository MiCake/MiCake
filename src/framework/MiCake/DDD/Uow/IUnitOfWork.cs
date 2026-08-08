using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Unit of Work interface for managing database transactions.
    /// The unit of work is the sole persistence owner; every writable unit of work uses explicit transactions.
    /// When created, the UoW is immediately active.
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
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
        /// Indicates if this is a read-only unit of work. Read-only units of work reject resource flush and write activation.
        /// </summary>
        bool IsReadOnly { get; }

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
        /// Flushes every registered resource in registration order and returns the sum of affected rows.
        /// Does not commit, complete the UoW, or raise commit events.
        /// Rejected on read-only units of work. If any flush fails, the unit of work becomes rollback-only
        /// and no later resource is flushed.
        /// </summary>
        Task<int> FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all changes to the database.
        /// For nested UoW, this only marks as completed; actual commit happens at root level.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes.
        /// For nested UoW, this marks the root UoW as rollback-only.
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
        /// Every currently registered resource must have an active transaction before the savepoint is created.
        /// </summary>
        /// <param name="name">Name of the savepoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The name of the created savepoint</returns>
        Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back to a specific savepoint, discarding changes made after that point.
        /// The savepoint remains valid and can be rolled back to again.
        /// Resources registered after the savepoint was created are rejected before any state changes.
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
        /// Event raised once before the first physical resource commit.
        /// A handler failure aborts the commit and triggers rollback of all eligible resources.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnCommitting;

        /// <summary>
        /// Event raised only after a complete successful commit.
        /// Not raised for partial commits or rollback failures. Shared nested completion raises no physical commit events.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnCommitted;

        /// <summary>
        /// Event raised once before rollback attempts of eligible resources.
        /// Handler failures are collected and surfaced with the rollback outcome.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;

        /// <summary>
        /// Event raised only after a complete successful rollback.
        /// Not raised for rollback failures or partial commits. Shared nested completion raises no physical rollback events.
        /// </summary>
        event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;

        #endregion
    }
}
