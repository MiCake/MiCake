using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// EF Core specific implementation of IUnitOfWorkResource using two-phase registration pattern.
    /// Phase 1 (Prepare): Store configuration synchronously
    /// Phase 2 (Activate): Start database transaction asynchronously
    /// </summary>
    public class EFCoreDbContextWrapper : IUnitOfWorkResource
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<EFCoreDbContextWrapper> _logger;
        private readonly bool _shouldDisposeDbContext;
        private readonly MiCakeEFCoreOptions _options;
        private IDbContextTransaction? _currentTransaction;
        private bool _disposed = false;

        // Two-phase registration state
        private bool _isPrepared = false;
        private bool _isInitialized = false;
        private IsolationLevel? _preparedIsolationLevel = null;

        /// <summary>
        /// Gets the underlying DbContext instance
        /// </summary>
        public DbContext DbContext => _dbContext;
        
        /// <summary>
        /// Gets the unique identifier for this resource instance
        /// </summary>
        public string ResourceIdentifier => $"{_dbContext.GetType().FullName}_{_dbContext.GetHashCode()}";
        
        /// <summary>
        /// Indicates if there's an active transaction
        /// </summary>
        public bool HasActiveTransaction => _currentTransaction != null;

        /// <summary>
        /// Indicates if the resource has been initialized (prepared and activated)
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Creates a new EF Core DbContext wrapper
        /// </summary>
        /// <param name="dbContext">The DbContext instance to wrap</param>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="options">EF Core options</param>
        /// <param name="shouldDisposeDbContext">Whether to dispose DbContext when wrapper is disposed</param>
        public EFCoreDbContextWrapper(
            DbContext dbContext,
            ILogger<EFCoreDbContextWrapper> logger,
            MiCakeEFCoreOptions options,
            bool shouldDisposeDbContext = false)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shouldDisposeDbContext = shouldDisposeDbContext;
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // Check if DbContext already has a transaction (user-managed)
            _currentTransaction = dbContext.Database.CurrentTransaction;

            _logger.LogDebug("Created EFCoreDbContextWrapper for {DbContextType}, ShouldDispose: {ShouldDispose}",
                _dbContext.GetType().Name, _shouldDisposeDbContext);
        }

        /// <summary>
        /// Prepares the resource for transaction (Phase 1 - Synchronous, no I/O).
        /// Just stores configuration, doesn't start actual transaction.
        /// </summary>
        public void PrepareForTransaction(IsolationLevel? isolationLevel)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isPrepared)
            {
                _logger.LogDebug("Resource {ResourceIdentifier} already prepared", ResourceIdentifier);
                return;
            }

            // Check if DbContext already has a transaction (user-managed)
            if (_currentTransaction != null)
            {
                _logger.LogDebug("Resource {ResourceIdentifier} already has user-managed transaction", ResourceIdentifier);
                _isPrepared = true;
                _isInitialized = true;  // User-managed transaction counts as initialized
                return;
            }

            // Store isolation level for later activation
            _preparedIsolationLevel = isolationLevel;
            _isPrepared = true;

            _logger.LogDebug("Resource {ResourceIdentifier} prepared for transaction with isolation level {IsolationLevel}", 
                ResourceIdentifier, isolationLevel);
        }

        /// <summary>
        /// Activates the transaction asynchronously (Phase 2 - May involve I/O).
        /// This is where the actual database transaction is started.
        /// </summary>
        public async Task ActivateTransactionAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_isPrepared)
            {
                throw new InvalidOperationException(
                    $"Resource {ResourceIdentifier} must be prepared before activation. " +
                    "Call PrepareForTransaction() first.");
            }

            if (_isInitialized)
            {
                _logger.LogDebug("Resource {ResourceIdentifier} already activated", ResourceIdentifier);
                return;
            }

            // If user-managed transaction exists, just mark as initialized
            if (_currentTransaction != null)
            {
                _isInitialized = true;
                return;
            }

            // ✅ Start the actual transaction (async, may involve I/O)
            _logger.LogDebug("Activating transaction for resource {ResourceIdentifier} with isolation level {IsolationLevel}", 
                ResourceIdentifier, _preparedIsolationLevel);

            try
            {
                if (_preparedIsolationLevel.HasValue)
                {
                    _currentTransaction = await _dbContext.Database
                        .BeginTransactionAsync(_preparedIsolationLevel.Value, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    _currentTransaction = await _dbContext.Database
                        .BeginTransactionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                _isInitialized = true;
                
                _logger.LogDebug("Successfully activated transaction for resource {ResourceIdentifier}", ResourceIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate transaction for resource {ResourceIdentifier}", ResourceIdentifier);
                throw;
            }
        }

        /// <summary>
        /// Begin a new transaction with specified isolation level.
        /// For new code, use PrepareForTransaction + ActivateTransactionAsync instead.
        /// </summary>
        public async Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken = default)
        {
            PrepareForTransaction(isolationLevel);
            await ActivateTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!HasActiveTransaction)
            {
                _logger.LogWarning("No active transaction to commit for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

            try
            {
                _logger.LogDebug("Committing transaction for DbContext {DbContextType}", _dbContext.GetType().Name);
                await _currentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            if (!HasActiveTransaction)
            {
                _logger.LogDebug("No active transaction to rollback for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

            try
            {
                _logger.LogDebug("Rolling back transaction for DbContext {DbContextType}", _dbContext.GetType().Name);
                await _currentTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transaction rollback for DbContext {DbContextType}", _dbContext.GetType().Name);
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Save changes to the underlying data store
        /// </summary>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _logger.LogDebug("Saving changes for DbContext {DbContextType}", _dbContext.GetType().Name);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a savepoint for partial rollback
        /// </summary>
        public async Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!HasActiveTransaction)
            {
                throw new InvalidOperationException(
                    $"Cannot create savepoint '{name}' - no active transaction exists for DbContext {_dbContext.GetType().Name}");
            }

            _logger.LogDebug("Creating savepoint '{SavepointName}' for DbContext {DbContextType}", name, _dbContext.GetType().Name);
            await _currentTransaction!.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
            return name;
        }

        /// <summary>
        /// Rollback to a specific savepoint
        /// </summary>
        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!HasActiveTransaction)
            {
                throw new InvalidOperationException(
                    $"Cannot rollback to savepoint '{name}' - no active transaction exists for DbContext {_dbContext.GetType().Name}");
            }

            _logger.LogDebug("Rolling back to savepoint '{SavepointName}' for DbContext {DbContextType}", name, _dbContext.GetType().Name);
            await _currentTransaction!.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Release a savepoint
        /// </summary>
        public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!HasActiveTransaction)
            {
                throw new InvalidOperationException(
                    $"Cannot release savepoint '{name}' - no active transaction exists for DbContext {_dbContext.GetType().Name}");
            }

            _logger.LogDebug("Releasing savepoint '{SavepointName}' for DbContext {DbContextType}", name, _dbContext.GetType().Name);
            await _currentTransaction!.ReleaseSavepointAsync(name, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _logger.LogDebug("Disposing EFCoreDbContextWrapper for {DbContextType}", _dbContext.GetType().Name);

            // Always dispose the transaction
            _currentTransaction?.Dispose();
            _currentTransaction = null;

            // Only dispose DbContext if explicitly allowed (e.g., when created by the factory for this specific UoW)
            if (_shouldDisposeDbContext)
            {
                try
                {
                    _dbContext?.Dispose();
                    _logger.LogDebug("Disposed DbContext {DbContextType} as requested", _dbContext.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing DbContext {DbContextType}", _dbContext.GetType().Name);
                }
            }
            else
            {
                _logger.LogDebug("DbContext {DbContextType} not disposed - managed by DI container", _dbContext.GetType().Name);
            }

            _disposed = true;
        }
    }
}
