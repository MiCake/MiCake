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
        /// </summary>
        /// <param name="resource">The resource to register</param>
        void RegisterResource(IUnitOfWorkResource resource);
    }
    
    /// <summary>
    /// Generic resource interface for UoW participation.
    /// Persistence layer implementations should implement this to integrate with UoW.
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
        /// Begin a new transaction with specified isolation level
        /// </summary>
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
