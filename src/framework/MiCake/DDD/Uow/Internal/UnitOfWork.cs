using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Clean implementation of Unit of Work that manages database transactions and contexts
    /// </summary>
    internal class UnitOfWork : IUnitOfWork
    {
        #region Fields

        private readonly List<IDbContextWrapper> _dbContexts = [];
        private readonly ILogger<UnitOfWork> _logger;
        private readonly Lock _lock = new();

        private bool _disposed = false;
        private bool _completed = false;
        private bool _transactionsStarted = false;
        private bool _skipCommit = false;

        #endregion

        #region Properties

        public Guid Id { get; }
        public bool IsDisposed => _disposed;
        public bool IsCompleted => _completed;
        public bool HasActiveTransactions => _transactionsStarted;

        #endregion

        public UnitOfWork(ILogger<UnitOfWork> logger)
        {
            Id = Guid.NewGuid();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Public Methods

        public void RegisterDbContext(IDbContextWrapper dbContextWrapper)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot register DbContext after unit of work is completed");
            ArgumentNullException.ThrowIfNull(dbContextWrapper);

            lock (_lock)
            {
                if (TryFindExistingWrapper(dbContextWrapper.ContextIdentifier, out _))
                {
                    _logger.LogDebug("DbContext with identifier {ContextIdentifier} already registered with UnitOfWork {UnitOfWorkId}, skipping duplicate registration",
                        dbContextWrapper.ContextIdentifier, Id);
                    return;
                }

                _dbContexts.Add(dbContextWrapper);
                _logger.LogDebug("DbContext with identifier {ContextIdentifier} registered with UnitOfWork {UnitOfWorkId} (SkipCommit: {SkipCommit})",
                    dbContextWrapper.ContextIdentifier, Id, _skipCommit);
            }
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot begin transactions after unit of work is completed");

            if (_transactionsStarted)
            {
                _logger.LogDebug("Transactions already started for UnitOfWork {UnitOfWorkId}", Id);
                return;
            }

            _logger.LogDebug("Beginning transactions for UnitOfWork {UnitOfWorkId} with {ContextCount} contexts", Id, _dbContexts.Count);

            var exceptions = await ExecuteOnAllWrappersAsync(
                wrapper => wrapper.BeginTransactionAsync(cancellationToken),
                "Failed to begin transaction for DbContext {0} in UnitOfWork {1}");

            if (exceptions.Count > 0)
            {
                await RollbackInternalAsync(cancellationToken);
                throw new AggregateException("Failed to begin transactions", exceptions);
            }

            _transactionsStarted = true;
            _logger.LogDebug("Successfully started transactions for UnitOfWork {UnitOfWorkId}", Id);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Unit of work has already been completed");

            if (_skipCommit)
            {
                _logger.LogDebug("Skipping commit for UnitOfWork {UnitOfWorkId} due to skip commit flag", Id);
                MarkAsCompleted();
                return;
            }

            _logger.LogDebug("Committing UnitOfWork {UnitOfWorkId} with {ContextCount} contexts", Id, _dbContexts.Count);

            var exceptions = await ExecuteCommitOperationsAsync(cancellationToken);

            if (exceptions.Count > 0)
            {
                if (_transactionsStarted)
                {
                    await RollbackInternalAsync(cancellationToken);
                }
                throw new AggregateException("Failed to commit unit of work", exceptions);
            }

            MarkAsCompleted();
            _logger.LogDebug("Successfully committed UnitOfWork {UnitOfWorkId}", Id);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return;

            ThrowIfCompleted("Cannot rollback a completed unit of work");
            await RollbackInternalAsync(cancellationToken);
        }

        public Task MarkAsCompletedAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_completed)
            {
                _logger.LogDebug("UnitOfWork {UnitOfWorkId} already completed", Id);
                return Task.CompletedTask;
            }

            _skipCommit = true;
            _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} to skip commit for performance optimization", Id);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogDebug("Disposing UnitOfWork {UnitOfWorkId} (Completed: {Completed}, SkipCommit: {SkipCommit})",
                Id, _completed, _skipCommit);

            HandleDisposalCleanup();
            DisposeAllWrappers();

            _dbContexts.Clear();
            _disposed = true;

            _logger.LogDebug("Disposed UnitOfWork {UnitOfWorkId}", Id);
        }

        #endregion

        #region Private Helper Methods

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void ThrowIfCompleted(string message)
        {
            if (_completed)
                throw new InvalidOperationException(message);
        }

        private bool TryFindExistingWrapper(string contextIdentifier, out IDbContextWrapper wrapper)
        {
            wrapper = _dbContexts.Find(w => w.ContextIdentifier == contextIdentifier);
            return wrapper != null;
        }

        private void MarkAsCompleted()
        {
            _completed = true;
        }

        private async Task<List<Exception>> ExecuteOnAllWrappersAsync(
            Func<IDbContextWrapper, Task> action,
            string errorMessageTemplate)
        {
            var exceptions = new List<Exception>();

            foreach (var wrapper in _dbContexts)
            {
                try
                {
                    await action(wrapper);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, errorMessageTemplate, wrapper.ContextIdentifier, Id);
                }
            }

            return exceptions;
        }

        private async Task<List<Exception>> ExecuteCommitOperationsAsync(CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();

            foreach (var wrapper in _dbContexts)
            {
                try
                {
                    await wrapper.SaveChangesAsync(cancellationToken);

                    // Only commit transaction if we have active transactions
                    if (_transactionsStarted && wrapper.HasActiveTransaction)
                    {
                        await wrapper.CommitTransactionAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to commit DbContext {ContextIdentifier} in UnitOfWork {UnitOfWorkId}",
                        wrapper.ContextIdentifier, Id);
                }
            }

            return exceptions;
        }

        private async Task RollbackInternalAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Rolling back UnitOfWork {UnitOfWorkId}", Id);

            var exceptions = await ExecuteOnAllWrappersAsync(
                async wrapper =>
                {
                    // Only rollback if we have active transactions
                    if (_transactionsStarted && wrapper.HasActiveTransaction)
                    {
                        await wrapper.RollbackTransactionAsync(cancellationToken);
                    }
                },
                "Failed to rollback DbContext {0} in UnitOfWork {1}");

            if (exceptions.Count > 0)
            {
                _logger.LogWarning("Some DbContexts failed to rollback in UnitOfWork {UnitOfWorkId}", Id);
            }

            _transactionsStarted = false;
        }

        private void HandleDisposalCleanup()
        {
            // If not completed and not marked to skip commit, try to rollback
            if (!_completed && !_skipCommit)
            {
                try
                {
                    RollbackAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback during disposal of UnitOfWork {UnitOfWorkId}", Id);
                }
            }
            else if (_skipCommit && !_completed)
            {
                // Mark as completed if skip commit was set but CommitAsync wasn't called
                MarkAsCompleted();
                _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} as completed during disposal due to skip commit flag", Id);
            }
        }

        private void DisposeAllWrappers()
        {
            foreach (var wrapper in _dbContexts)
            {
                try
                {
                    wrapper.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispose DbContext wrapper in UnitOfWork {UnitOfWorkId}", Id);
                }
            }
        }

        #endregion
    }
}
