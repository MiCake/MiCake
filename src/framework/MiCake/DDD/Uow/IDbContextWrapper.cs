using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Interface for database context wrapper that provides transaction management
    /// </summary>
    public interface IDbContextWrapper : IDisposable
    {
        /// <summary>
        /// Unique identifier for the underlying database context instance.
        /// Used for comparing wrappers to prevent duplicate registrations.
        /// </summary>
        string ContextIdentifier { get; }

        /// <summary>
        /// Indicates if there's an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Begin a new transaction with specified isolation level
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task BeginTransactionAsync(IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Save changes to the database
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
