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
    /// EF Core specific implementation of IUnitOfWorkResource that handles DbContext lifecycle properly
    /// </summary>
    public class EFCoreDbContextWrapper : IUnitOfWorkResource
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<EFCoreDbContextWrapper> _logger;
        private readonly bool _shouldDisposeDbContext;
        private MiCakeEFCoreOptions _options;
        private IDbContextTransaction _currentTransaction;
        private bool _disposed = false;

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

            // Check if DbContext already has a transaction
            _currentTransaction = dbContext.Database.CurrentTransaction;

            _logger.LogDebug("Created EFCoreDbContextWrapper for {DbContextType}, ShouldDispose: {ShouldDispose}",
                _dbContext.GetType().Name, _shouldDisposeDbContext);
        }

        /// <summary>
        /// Begin a new transaction with specified isolation level
        /// </summary>
        public async Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Check if transactions are disabled for UoW to improve performance
            if (_options != null && !_options.WillOpenTransactionForUow)
            {
                _logger.LogDebug("Transaction creation disabled for UoW (WillOpenTransactionForUow = false) for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

            if (HasActiveTransaction)
            {
                _logger.LogDebug("Transaction already active for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

            _logger.LogDebug("Beginning transaction for DbContext {DbContextType} with isolation level {IsolationLevel}", 
                _dbContext.GetType().Name, isolationLevel);
            
            if (isolationLevel.HasValue)
            {
                _currentTransaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // If transactions are disabled for UoW, nothing to commit
            if (_options != null && !_options.WillOpenTransactionForUow)
            {
                _logger.LogDebug("Transaction commit skipped (WillOpenTransactionForUow = false) for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

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

            // If transactions are disabled for UoW, nothing to rollback
            if (_options != null && !_options.WillOpenTransactionForUow)
            {
                _logger.LogDebug("Transaction rollback skipped (WillOpenTransactionForUow = false) for DbContext {DbContextType}", _dbContext.GetType().Name);
                return;
            }

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
