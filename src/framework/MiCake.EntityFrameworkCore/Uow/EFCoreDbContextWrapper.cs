using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// EF Core specific implementation of IDbContextWrapper that handles DbContext lifecycle properly
    /// </summary>
    public class EFCoreDbContextWrapper : IDbContextWrapper
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<EFCoreDbContextWrapper> _logger;
        private readonly bool _shouldDisposeDbContext;
        private MiCakeEFCoreOptions _options;
        private IDbContextTransaction _currentTransaction;
        private bool _disposed = false;

        public DbContext DbContext => _dbContext;
        public string ContextIdentifier => $"{_dbContext.GetType().FullName}_{_dbContext.GetHashCode()}";
        public bool HasActiveTransaction => _currentTransaction != null;

        public EFCoreDbContextWrapper(
            DbContext dbContext,
            ILogger<EFCoreDbContextWrapper> logger,
            MiCakeEFCoreOptions options,
            bool shouldDisposeDbContext = false)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger;
            _shouldDisposeDbContext = shouldDisposeDbContext;
            _options = options;

            // Check if DbContext already has a transaction
            _currentTransaction = dbContext.Database.CurrentTransaction;

            _logger.LogDebug("Created EFCoreDbContextWrapper for {DbContextType}, ShouldDispose: {ShouldDispose}",
                _dbContext.GetType().Name, _shouldDisposeDbContext);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
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

            _logger.LogDebug("Beginning transaction for DbContext {DbContextType}", _dbContext.GetType().Name);
            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
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
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
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
                await _currentTransaction.RollbackAsync(cancellationToken);
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

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _logger.LogDebug("Saving changes for DbContext {DbContextType}", _dbContext.GetType().Name);
            return await _dbContext.SaveChangesAsync(cancellationToken);
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
