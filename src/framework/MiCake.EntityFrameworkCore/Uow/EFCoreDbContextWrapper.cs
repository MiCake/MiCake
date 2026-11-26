using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// EF Core implementation of <see cref="IUnitOfWorkResource"/>.
    /// Binds one DbContext instance to one unit of work: <see cref="Prepare"/> is idempotent per UoW
    /// and rejects rebinding to another live UoW. Transaction activation is idempotent and lazy;
    /// commit and rollback are terminal and at most once. Provider failures propagate to the UoW
    /// boundary so the boundary can compose primary, rollback, and cleanup failures.
    /// </summary>
    public class EFCoreDbContextWrapper : IUnitOfWorkResource
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<EFCoreDbContextWrapper> _logger;
        private readonly bool _shouldDisposeDbContext;

        private IDbContextTransaction? _currentTransaction;
        private bool _disposed;
        private bool _isPrepared;
        private bool _commitAttempted;
        private bool _commitSucceeded;
        private bool _rollbackAttempted;
        private bool _transactionCompleted;
        private Guid _preparedUowId;
        private IsolationLevel? _preparedIsolationLevel;

        /// <summary>
        /// Collision-safe identity of this resource instance. Generated once per wrapper and never
        /// derived from <see cref="object.GetHashCode"/>.
        /// </summary>
        public UnitOfWorkResourceId Id { get; } = new(Guid.NewGuid());

        /// <summary>
        /// Stable type name used in diagnostics and commit outcomes.
        /// </summary>
        public string ResourceType => _dbContext.GetType().FullName ?? _dbContext.GetType().Name;

        /// <summary>
        /// Indicates whether the resource currently holds an active transaction.
        /// </summary>
        public bool HasActiveTransaction => _currentTransaction != null && !_transactionCompleted;

        /// <summary>
        /// Indicates whether the resource currently supports savepoints. EF Core exposes savepoint
        /// capability on the active provider transaction; without an active transaction savepoints
        /// are not supported.
        /// </summary>
        public bool SupportsSavepoints => _currentTransaction?.SupportsSavepoints ?? false;

        /// <summary>
        /// The wrapped DbContext instance.
        /// </summary>
        public DbContext DbContext => _dbContext;

        /// <summary>
        /// Creates a new EF Core DbContext wrapper.
        /// </summary>
        /// <param name="dbContext">The DbContext instance to wrap.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="shouldDisposeDbContext">Whether to dispose the DbContext when the wrapper is disposed.</param>
        public EFCoreDbContextWrapper(
            DbContext dbContext,
            ILogger<EFCoreDbContextWrapper> logger,
            bool shouldDisposeDbContext = false)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shouldDisposeDbContext = shouldDisposeDbContext;

            _logger.LogDebug("Created EFCoreDbContextWrapper for {DbContextType}, ShouldDispose: {ShouldDispose}",
                _dbContext.GetType().Name, _shouldDisposeDbContext);
        }

        /// <summary>
        /// Prepares the resource for participation in the given unit of work.
        /// Synchronous and I/O-free. Idempotent only for the same unit of work;
        /// rebinding to another live unit of work is rejected.
        /// </summary>
        public void Prepare(UnitOfWorkResourceContext context)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(context);

            if (_isPrepared)
            {
                if (_preparedUowId != context.UnitOfWorkId)
                {
                    throw new InvalidOperationException(
                        $"Resource {Id} ({ResourceType}) is already bound to unit of work {_preparedUowId} " +
                        $"and cannot be rebound to another live unit of work {context.UnitOfWorkId}.");
                }

                return;
            }

            _preparedUowId = context.UnitOfWorkId;
            _preparedIsolationLevel = context.IsolationLevel;
            _isPrepared = true;

            var preparedIsReadOnly = context.IsReadOnly;
            _logger.LogDebug(
                "Prepared resource {ResourceId} for UoW {UnitOfWorkId} (IsolationLevel: {IsolationLevel}, ReadOnly: {ReadOnly})",
                Id, _preparedUowId, _preparedIsolationLevel, preparedIsReadOnly);
        }

        /// <summary>
        /// Synchronously ensures an active transaction. Idempotent.
        /// </summary>
        public void EnsureTransaction()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = _preparedIsolationLevel.HasValue
                ? _dbContext.Database.BeginTransaction(_preparedIsolationLevel.Value)
                : _dbContext.Database.BeginTransaction();

            _logger.LogDebug("Activated transaction for resource {ResourceId} in UoW {UnitOfWorkId}",
                Id, _preparedUowId);
        }

        /// <summary>
        /// Asynchronously ensures an active transaction. Idempotent.
        /// </summary>
        public async ValueTask EnsureTransactionAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = _preparedIsolationLevel.HasValue
                ? await _dbContext.Database.BeginTransactionAsync(_preparedIsolationLevel.Value, cancellationToken).ConfigureAwait(false)
                : await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Activated transaction for resource {ResourceId} in UoW {UnitOfWorkId}",
                Id, _preparedUowId);
        }

        /// <summary>
        /// Flushes pending changes to the data store without committing.
        /// Legal only after prepare and transaction activation; without an active transaction the
        /// flush would fall back to EF's implicit transaction and bypass the UoW boundary.
        /// </summary>
        public async ValueTask<int> FlushAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction == null)
            {
                throw new InvalidOperationException(
                    $"Resource {Id} ({ResourceType}) cannot flush without an active transaction. " +
                    "EnsureTransaction or EnsureTransactionAsync must succeed before flushing.");
            }

            _logger.LogDebug("Flushing resource {ResourceId} ({DbContextType})", Id, ResourceType);
            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Commits the transaction. Terminal and at most once.
        /// </summary>
        public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();
            EnsureCanCommit();

            _commitAttempted = true;

            if (_currentTransaction == null)
            {
                _logger.LogDebug("No active transaction to commit for resource {ResourceId}", Id);
                return;
            }

            try
            {
                await _currentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _commitSucceeded = true;
                _transactionCompleted = true;
                _logger.LogDebug("Committed transaction for resource {ResourceId}", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction for resource {ResourceId}", Id);
                throw;
            }
        }

        /// <summary>
        /// Rolls back the transaction. Terminal and at most once. Failures propagate to the UoW boundary.
        /// </summary>
        public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();
            EnsureCanRollback();

            _rollbackAttempted = true;

            if (_currentTransaction == null)
            {
                _logger.LogDebug("No active transaction to roll back for resource {ResourceId}", Id);
                return;
            }

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _transactionCompleted = true;
                _logger.LogDebug("Rolled back transaction for resource {ResourceId}", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to roll back transaction for resource {ResourceId}", Id);
                throw;
            }
        }

        /// <summary>
        /// Creates a savepoint on the active transaction when the provider supports it.
        /// Throws <see cref="NotSupportedException"/> before changing any state when no
        /// transaction is active or the provider does not support savepoints.
        /// </summary>
        public ValueTask CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction == null)
            {
                throw new NotSupportedException(
                    $"Resource {Id} ({ResourceType}) does not support savepoints without an active transaction.");
            }

            return new ValueTask(_currentTransaction.CreateSavepointAsync(name, cancellationToken));
        }

        /// <summary>
        /// Rolls back to a savepoint on the active transaction when the provider supports it.
        /// Throws <see cref="NotSupportedException"/> before changing any state when no
        /// transaction is active or the provider does not support savepoints.
        /// </summary>
        public ValueTask RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction == null)
            {
                throw new NotSupportedException(
                    $"Resource {Id} ({ResourceType}) does not support savepoints without an active transaction.");
            }

            return new ValueTask(_currentTransaction.RollbackToSavepointAsync(name, cancellationToken));
        }

        /// <summary>
        /// Releases a savepoint on the active transaction when the provider supports it.
        /// Throws <see cref="NotSupportedException"/> before changing any state when no
        /// transaction is active or the provider does not support savepoints.
        /// </summary>
        public ValueTask ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsurePrepared();

            if (_currentTransaction == null)
            {
                throw new NotSupportedException(
                    $"Resource {Id} ({ResourceType}) does not support savepoints without an active transaction.");
            }

            return new ValueTask(_currentTransaction.ReleaseSavepointAsync(name, cancellationToken));
        }

        /// <summary>
        /// Disposes the current transaction and, when owned, the wrapped DbContext.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _currentTransaction?.Dispose();
            }
            finally
            {
                _currentTransaction = null;
            }

            if (_shouldDisposeDbContext)
            {
                _dbContext.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously disposes the current transaction and, when owned, the wrapped DbContext.
        /// Both cleanup failures are collected and surfaced so the UoW boundary can report them.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var cleanupFailures = new List<Exception>();

            if (_currentTransaction != null)
            {
                var transaction = _currentTransaction;
                _currentTransaction = null;
                try
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailures.Add(ex);
                    _logger.LogError(ex, "Failed to dispose transaction for resource {ResourceId}", Id);
                }
            }

            if (_shouldDisposeDbContext)
            {
                try
                {
                    await _dbContext.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailures.Add(ex);
                    _logger.LogError(ex, "Failed to dispose DbContext for resource {ResourceId}", Id);
                }
            }

            GC.SuppressFinalize(this);

            if (cleanupFailures.Count > 0)
            {
                throw new UnitOfWorkBoundaryException(
                    $"Resource {Id} ({ResourceType}) cleanup failed during disposal.",
                    cleanupExceptions: cleanupFailures);
            }
        }

        private void EnsurePrepared()
        {
            if (!_isPrepared)
            {
                throw new InvalidOperationException(
                    $"Resource {Id} ({ResourceType}) must be prepared before use.");
            }
        }

        private void EnsureCanCommit()
        {
            if (_commitAttempted || _rollbackAttempted)
            {
                throw new InvalidOperationException(
                    $"Resource {Id} commit is terminal and at most once.");
            }
        }

        private void EnsureCanRollback()
        {
            if (_commitSucceeded || _rollbackAttempted)
            {
                throw new InvalidOperationException(
                    $"Resource {Id} rollback is terminal and at most once, and cannot follow a successful commit.");
            }
        }
    }
}
