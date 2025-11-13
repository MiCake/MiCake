using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Internal interface for framework components to register with UoW.
    /// This is NOT exposed in the public API and should only be used by persistence layer implementations.
    /// </summary>
    public interface IUnitOfWorkInternal
    {
        /// <summary>
        /// Register a resource (like DbContext) with this unit of work.
        /// Only called by framework components (e.g., EFCoreContextFactory).
        /// This is a synchronous, lightweight operation that prepares the resource.
        /// </summary>
        /// <param name="resource">The resource to register</param>
        void RegisterResource(IUnitOfWorkResource resource);

        /// <summary>
        /// Activates all pending resources asynchronously.
        /// This starts actual database transactions for all registered resources.
        /// Called before commit/rollback or immediately in Immediate initialization mode.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Generic resource interface for UoW participation using two-phase registration pattern.
    /// Persistence layer implementations should implement this to integrate with UoW.
    /// 
    /// Two-Phase Pattern:
    /// 1. Prepare Phase (Synchronous): Store configuration, no I/O operations
    /// 2. Activate Phase (Asynchronous): Start actual database transactions
    /// </summary>
    public interface IUnitOfWorkResource : IDisposable
    {
        /// <summary>
        /// Unique identifier for this resource instance
        /// </summary>
        string ResourceIdentifier { get; }
        
        /// <summary>
        /// Indicates if there's an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Indicates if the resource has been initialized (prepared and activated)
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Prepares the resource for transaction (synchronous, lightweight).
        /// This is called during registration and should NOT perform I/O operations.
        /// Only stores configuration like isolation level for later activation.
        /// </summary>
        /// <param name="isolationLevel">The isolation level to use for the transaction</param>
        void PrepareForTransaction(IsolationLevel? isolationLevel);

        /// <summary>
        /// Activates the transaction asynchronously (may involve I/O).
        /// This is where the actual database transaction is started.
        /// Called lazily when first operation is performed (Lazy mode),
        /// or immediately via lifecycle hooks (Immediate mode).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ActivateTransactionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Begin a new transaction with specified isolation level.
        /// This method combines PrepareForTransaction + ActivateTransactionAsync for backward compatibility.
        /// For new code, prefer using the two-phase pattern directly.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Save changes to the underlying data store
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Create a savepoint for partial rollback
        /// </summary>
        Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Rollback to a specific savepoint
        /// </summary>
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Release a savepoint
        /// </summary>
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
    }
}
