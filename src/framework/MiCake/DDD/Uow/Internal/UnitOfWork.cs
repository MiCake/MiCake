using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Implementation of Unit of Work with nested transaction support.
    /// When created, the UoW is immediately active. Transactions are automatically started if configured.
    /// </summary>
    internal class UnitOfWork : IUnitOfWork
    {
        #region Fields

        private readonly List<IDbContextWrapper> _dbContexts = [];
        private readonly ILogger<UnitOfWork> _logger;
        private readonly UnitOfWorkOptions _options;
        private readonly Lock _lock = new();

        private bool _disposed = false;
        private bool _completed = false;
        private bool _transactionsStarted = false;
        private bool _skipCommit = false;
        private bool _shouldRollback = false;  // For nested UoW to signal parent to rollback

        #endregion

        #region Properties

        public Guid Id { get; }
        public bool IsDisposed => _disposed;
        public bool IsCompleted => _completed;
        public bool HasActiveTransactions => _transactionsStarted;
        public IsolationLevel? IsolationLevel => _options.IsolationLevel;
        public IUnitOfWork? Parent { get; }

        #endregion

        public UnitOfWork(
            ILogger<UnitOfWork> logger,
            UnitOfWorkOptions? options = null,
            IUnitOfWork? parent = null)
        {
            Id = Guid.NewGuid();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? UnitOfWorkOptions.Default;
            Parent = parent;

            if (Parent != null)
            {
                _logger.LogDebug("Created nested UnitOfWork {UnitOfWorkId} with parent {ParentId}",
                    Id, Parent.Id);
            }
            else
            {
                _logger.LogDebug("Created root UnitOfWork {UnitOfWorkId} (IsolationLevel: {IsolationLevel}, AutoBegin: {AutoBegin})",
                    Id, _options.IsolationLevel, _options.AutoBeginTransaction);
            }
        }

        #region Public Methods

        public void RegisterDbContext(IDbContextWrapper dbContextWrapper)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot register DbContext after unit of work is completed");
            ArgumentNullException.ThrowIfNull(dbContextWrapper);

            // If nested, register with parent instead
            if (Parent != null)
            {
                Parent.RegisterDbContext(dbContextWrapper);
                return;
            }

            lock (_lock)
            {
                if (TryFindExistingWrapper(dbContextWrapper.ContextIdentifier, out _))
                {
                    _logger.LogDebug("DbContext with identifier {ContextIdentifier} already registered with UnitOfWork {UnitOfWorkId}",
                        dbContextWrapper.ContextIdentifier, Id);
                    return;
                }

                _dbContexts.Add(dbContextWrapper);
                _logger.LogDebug("DbContext with identifier {ContextIdentifier} registered with UnitOfWork {UnitOfWorkId}",
                    dbContextWrapper.ContextIdentifier, Id);

                // Auto-begin transaction if configured and this is first context
                if (_options.AutoBeginTransaction && !_transactionsStarted)
                {
                    Task.Run(async () => await BeginTransactionsInternalAsync(default).ConfigureAwait(false))
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Unit of work has already been completed");

            // If nested UoW, just mark as completed (parent will do actual commit)
            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} completed successfully", Id);
                MarkAsCompleted();
                return;
            }

            // Root UoW: do actual commit
            if (_skipCommit || _options.IsReadOnly)
            {
                _logger.LogDebug("Skipping commit for UnitOfWork {UnitOfWorkId} (SkipCommit: {SkipCommit}, ReadOnly: {ReadOnly})",
                    Id, _skipCommit, _options.IsReadOnly);
                MarkAsCompleted();
                return;
            }

            if (_shouldRollback)
            {
                _logger.LogWarning("UnitOfWork {UnitOfWorkId} marked for rollback by nested UoW, performing rollback", Id);
                await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Cannot commit: nested unit of work requested rollback");
            }

            _logger.LogDebug("Committing UnitOfWork {UnitOfWorkId} with {ContextCount} contexts", Id, _dbContexts.Count);

            var exceptions = await ExecuteCommitOperationsAsync(cancellationToken).ConfigureAwait(false);

            if (exceptions.Count > 0)
            {
                if (_transactionsStarted)
                {
                    await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
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

            // If nested UoW, signal parent to rollback
            if (Parent != null)
            {
                _logger.LogWarning("Nested UnitOfWork {UnitOfWorkId} requesting parent {ParentId} to rollback",
                    Id, Parent.Id);
                
                // Signal parent via its internal state (we need to expose this through an internal method)
                if (Parent is UnitOfWork parentUow)
                {
                    parentUow._shouldRollback = true;
                }
                
                MarkAsCompleted();
                return;
            }

            // Root UoW: do actual rollback
            await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
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
            _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} to skip commit", Id);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogDebug("Disposing UnitOfWork {UnitOfWorkId} (Completed: {Completed}, Nested: {Nested})",
                Id, _completed, Parent != null);

            HandleDisposalCleanup();
            
            // Only dispose contexts if this is root UoW
            if (Parent == null)
            {
                DisposeAllWrappers();
                _dbContexts.Clear();
            }

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

        private bool TryFindExistingWrapper(string contextIdentifier, out IDbContextWrapper? wrapper)
        {
            wrapper = _dbContexts.Find(w => w.ContextIdentifier == contextIdentifier);
            return wrapper != null;
        }

        private void MarkAsCompleted()
        {
            _completed = true;
        }

        private async Task BeginTransactionsInternalAsync(CancellationToken cancellationToken)
        {
            if (_transactionsStarted || _options.IsReadOnly)
                return;

            _logger.LogDebug("Beginning transactions for UnitOfWork {UnitOfWorkId} with {ContextCount} contexts (IsolationLevel: {IsolationLevel})",
                Id, _dbContexts.Count, _options.IsolationLevel);

            var exceptions = await ExecuteOnAllWrappersAsync(
                wrapper => wrapper.BeginTransactionAsync(_options.IsolationLevel, cancellationToken),
                "Failed to begin transaction for DbContext {0} in UnitOfWork {1}").ConfigureAwait(false);

            if (exceptions.Count > 0)
            {
                await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                throw new AggregateException("Failed to begin transactions", exceptions);
            }

            _transactionsStarted = true;
            _logger.LogDebug("Successfully started transactions for UnitOfWork {UnitOfWorkId}", Id);
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
                    await action(wrapper).ConfigureAwait(false);
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
                    await wrapper.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    // Only commit transaction if we have active transactions
                    if (_transactionsStarted && wrapper.HasActiveTransaction)
                    {
                        await wrapper.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
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
                        await wrapper.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
                    }
                },
                "Failed to rollback DbContext {0} in UnitOfWork {1}").ConfigureAwait(false);

            if (exceptions.Count > 0)
            {
                _logger.LogWarning("Some DbContexts failed to rollback in UnitOfWork {UnitOfWorkId}", Id);
            }

            _transactionsStarted = false;
        }

        private void HandleDisposalCleanup()
        {
            // If not completed and not marked to skip commit, try to rollback
            if (!_completed && !_skipCommit && Parent == null)
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
                _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} as completed during disposal", Id);
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
