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
    internal class UnitOfWork : IUnitOfWork, IUnitOfWorkInternal
    {
        #region Fields

        private readonly List<IUnitOfWorkResource> _resources = [];
        private readonly ILogger<UnitOfWork> _logger;
        private readonly UnitOfWorkOptions _options;
        private readonly Lock _lock = new();

        private bool _disposed = false;
        private bool _completed = false;
        private bool _transactionsStarted = false;
        private bool _skipCommit = false;
        private bool _shouldRollback = false;

        #endregion

        #region Properties

        public Guid Id { get; }
        public bool IsDisposed => _disposed;
        public bool IsCompleted => _completed;
        public bool HasActiveTransactions => _transactionsStarted;
        public IsolationLevel? IsolationLevel => _options.IsolationLevel;
        public IUnitOfWork? Parent { get; }

        #endregion

        #region Events

        public event EventHandler<UnitOfWorkEventArgs>? OnCommitting;
        public event EventHandler<UnitOfWorkEventArgs>? OnCommitted;
        public event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;
        public event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;

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
                _logger.LogDebug("Created root UnitOfWork {UnitOfWorkId} (IsolationLevel: {IsolationLevel}, InitMode: {InitMode}, ReadOnly: {ReadOnly})",
                    Id, _options.IsolationLevel, _options.InitializationMode, _options.IsReadOnly);
            }
        }

        #region Public Methods

        public void RegisterResource(IUnitOfWorkResource resource)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot register resource after unit of work is completed");
            ArgumentNullException.ThrowIfNull(resource);

            // If nested, register with parent instead
            if (Parent is IUnitOfWorkInternal parentInternal)
            {
                parentInternal.RegisterResource(resource);
                return;
            }

            lock (_lock)
            {
                if (TryFindExistingResource(resource.ResourceIdentifier, out _))
                {
                    _logger.LogDebug("Resource with identifier {ResourceIdentifier} already registered with UnitOfWork {UnitOfWorkId}",
                        resource.ResourceIdentifier, Id);
                    return;
                }

                _resources.Add(resource);
                _logger.LogDebug("Resource with identifier {ResourceIdentifier} registered with UnitOfWork {UnitOfWorkId}",
                    resource.ResourceIdentifier, Id);

                // ✅ Two-Phase Registration: Phase 1 - Prepare (synchronous, no I/O)
                // Store configuration for later activation
                try
                {
                    resource.PrepareForTransaction(_options.IsolationLevel);
                    _logger.LogDebug("Resource {ResourceIdentifier} prepared for transaction with isolation level {IsolationLevel}",
                        resource.ResourceIdentifier, _options.IsolationLevel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to prepare resource {ResourceIdentifier} for transaction", resource.ResourceIdentifier);
                    _resources.Remove(resource);
                    throw;
                }
            }
        }

        /// <summary>
        /// Activates all pending resources asynchronously (Phase 2 of two-phase registration).
        /// This starts actual database transactions for all registered resources.
        /// </summary>
        public async Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (_transactionsStarted)
            {
                _logger.LogDebug("Transactions already started for UnitOfWork {UnitOfWorkId}", Id);
                return;
            }

            // Nested UoW: delegate to parent
            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} delegating activation to parent {ParentId}", 
                    Id, Parent.Id);
                
                if (Parent is IUnitOfWorkInternal parentInternal)
                {
                    await parentInternal.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            // Skip activation for read-only UoW or if no resources
            if (_options.IsReadOnly || _resources.Count == 0)
            {
                _logger.LogDebug("Skipping activation for UnitOfWork {UnitOfWorkId} (ReadOnly: {ReadOnly}, Resources: {Count})", 
                    Id, _options.IsReadOnly, _resources.Count);
                return;
            }

            _logger.LogDebug("Activating {Count} pending resources for UnitOfWork {UnitOfWorkId}", 
                _resources.Count, Id);

            var exceptions = new List<Exception>();
            
            // Activate all resources that haven't been initialized yet
            foreach (var resource in _resources)
            {
                if (!resource.IsInitialized)
                {
                    try
                    {
                        await resource.ActivateTransactionAsync(cancellationToken).ConfigureAwait(false);
                        _logger.LogDebug("Successfully activated resource {ResourceIdentifier}", 
                            resource.ResourceIdentifier);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        _logger.LogError(ex, "Failed to activate resource {ResourceIdentifier} in UnitOfWork {UnitOfWorkId}",
                            resource.ResourceIdentifier, Id);
                    }
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Failed to activate one or more resources", exceptions);
            }

            _transactionsStarted = true;
            _logger.LogDebug("Successfully activated all resources for UnitOfWork {UnitOfWorkId}", Id);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Unit of work has already been completed");

            // Raise OnCommitting event
            RaiseEvent(OnCommitting, new UnitOfWorkEventArgs(Id, Parent != null));

            try
            {
                // If nested UoW, just mark as completed (parent will do actual commit)
                if (Parent != null)
                {
                    _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} completed successfully", Id);
                    MarkAsCompleted();

                    // Raise OnCommitted event for nested UoW
                    RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
                    return;
                }

                // Root UoW: do actual commit
                if (_skipCommit || _options.IsReadOnly)
                {
                    _logger.LogDebug("Skipping commit for UnitOfWork {UnitOfWorkId} (SkipCommit: {SkipCommit}, ReadOnly: {ReadOnly})", Id, _skipCommit, _options.IsReadOnly);
                    MarkAsCompleted();

                    // Raise OnCommitted event even for skipped commit
                    RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
                    return;
                }

                if (_shouldRollback)
                {
                    _logger.LogWarning("UnitOfWork {UnitOfWorkId} marked for rollback by nested UoW, performing rollback", Id);
                    await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException("Cannot commit: nested unit of work requested rollback");
                }

                // ✅ Activate all pending resources (lazy initialization - Phase 2 of two-phase pattern)
                await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Committing UnitOfWork {UnitOfWorkId} with {ResourceCount} resources", Id, _resources.Count);

                var exceptions = await ExecuteCommitOperationsAsync(cancellationToken).ConfigureAwait(false);

                if (exceptions.Count > 0)
                {
                    if (_transactionsStarted)
                    {
                        await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                    }
                    
                    // If only one exception, unwrap it; otherwise wrap in aggregate
                    if (exceptions.Count == 1)
                    {
                        throw exceptions[0];
                    }
                    throw new AggregateException("Failed to commit unit of work", exceptions);
                }

                MarkAsCompleted();
                _logger.LogDebug("Successfully committed UnitOfWork {UnitOfWorkId}", Id);

                // Raise OnCommitted event
                RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
            }
            catch (Exception ex)
            {
                // Raise OnRolledBack event with exception
                RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null, ex));
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return;

            ThrowIfCompleted("Cannot rollback a completed unit of work");

            // Raise OnRollingBack event
            RaiseEvent(OnRollingBack, new UnitOfWorkEventArgs(Id, Parent != null));

            try
            {
                // If nested UoW, signal parent to rollback
                if (Parent != null)
                {
                    _logger.LogWarning("Nested UnitOfWork {UnitOfWorkId} requesting parent {ParentId} to rollback",
                        Id, Parent.Id);

                    // Signal parent via its internal state
                    if (Parent is UnitOfWork parentUow)
                    {
                        parentUow._shouldRollback = true;
                    }

                    MarkAsCompleted();

                    // Raise OnRolledBack event for nested UoW
                    RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null));
                    return;
                }

                // Root UoW: do actual rollback
                await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);

                MarkAsCompleted();

                // Raise OnRolledBack event
                RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null));
            }
            catch (Exception ex)
            {
                // Raise OnRolledBack event with exception
                RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null, ex));
                throw;
            }
        }

        public Task MarkAsCompletedAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_completed)
            {
                _logger.LogDebug("UnitOfWork {UnitOfWorkId} already completed", Id);
                return Task.CompletedTask;
            }

            MarkAsCompleted();
            _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} as completed", Id);
            return Task.CompletedTask;
        }

        #region Savepoint Support

        public async Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot create savepoint after unit of work is completed");
            
            // Generate a name if null or empty
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"sp_{Guid.NewGuid():N}";
                _logger.LogDebug("Generated savepoint name: {SavepointName}", name);
            }

            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} delegating savepoint creation to parent", Id);
                return await Parent.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            if (!_transactionsStarted)
            {
                throw new InvalidOperationException("Cannot create savepoint: no active transaction");
            }

            _logger.LogDebug("Creating savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            var exceptions = new List<Exception>();
            foreach (var resource in _resources)
            {
                try
                {
                    await resource.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to create savepoint '{SavepointName}' for resource {ResourceIdentifier}",
                        name, resource.ResourceIdentifier);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to create savepoint '{name}'", exceptions);
            }

            return name;
        }

        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot rollback to savepoint after unit of work is completed");
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} delegating savepoint rollback to parent", Id);
                await Parent.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                return;
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            if (!_transactionsStarted)
            {
                throw new InvalidOperationException("Cannot rollback to savepoint: no active transaction");
            }

            _logger.LogDebug("Rolling back to savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            var exceptions = new List<Exception>();
            foreach (var resource in _resources)
            {
                try
                {
                    await resource.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to rollback to savepoint '{SavepointName}' for resource {ResourceIdentifier}",
                        name, resource.ResourceIdentifier);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to rollback to savepoint '{name}'", exceptions);
            }
        }

        public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot release savepoint after unit of work is completed");
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} delegating savepoint release to parent", Id);
                await Parent.ReleaseSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                return;
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            if (!_transactionsStarted)
            {
                throw new InvalidOperationException("Cannot release savepoint: no active transaction");
            }

            _logger.LogDebug("Releasing savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            var exceptions = new List<Exception>();
            foreach (var resource in _resources)
            {
                try
                {
                    await resource.ReleaseSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to release savepoint '{SavepointName}' for resource {ResourceIdentifier}",
                        name, resource.ResourceIdentifier);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to release savepoint '{name}'", exceptions);
            }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogDebug("Disposing UnitOfWork {UnitOfWorkId} (Completed: {Completed}, Nested: {Nested})",
                Id, _completed, Parent != null);

            HandleDisposalCleanup();

            // Only dispose resources if this is root UoW
            if (Parent == null)
            {
                DisposeAllResources();
                _resources.Clear();
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

        private bool TryFindExistingResource(string resourceIdentifier, out IUnitOfWorkResource? resource)
        {
            resource = _resources.Find(w => w.ResourceIdentifier == resourceIdentifier);
            return resource != null;
        }

        private void MarkAsCompleted()
        {
            _completed = true;
        }

        private void RaiseEvent(EventHandler<UnitOfWorkEventArgs>? eventHandler, UnitOfWorkEventArgs args)
        {
            try
            {
                eventHandler?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising event in UnitOfWork {UnitOfWorkId}", Id);
                // Don't throw - event handler errors shouldn't break UoW flow
            }
        }

        private async Task<List<Exception>> ExecuteOnAllResourcesAsync(
            Func<IUnitOfWorkResource, Task> action,
            string errorMessageTemplate)
        {
            var exceptions = new List<Exception>();

            foreach (var resource in _resources)
            {
                try
                {
                    await action(resource).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, errorMessageTemplate, resource.ResourceIdentifier, Id);
                }
            }

            return exceptions;
        }

        private async Task<List<Exception>> ExecuteCommitOperationsAsync(CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();

            foreach (var resource in _resources)
            {
                try
                {
                    await resource.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    // Only commit transaction if we have active transactions
                    if (_transactionsStarted && resource.HasActiveTransaction)
                    {
                        await resource.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to commit resource {ResourceIdentifier} in UnitOfWork {UnitOfWorkId}",
                        resource.ResourceIdentifier, Id);
                }
            }

            return exceptions;
        }

        private async Task RollbackInternalAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Rolling back UnitOfWork {UnitOfWorkId}", Id);

            var exceptions = await ExecuteOnAllResourcesAsync(
                async resource =>
                {
                    // Only rollback if we have active transactions
                    if (_transactionsStarted && resource.HasActiveTransaction)
                    {
                        await resource.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    }
                },
                "Failed to rollback resource {0} in UnitOfWork {1}").ConfigureAwait(false);

            if (exceptions.Count > 0)
            {
                _logger.LogWarning("Some resources failed to rollback in UnitOfWork {UnitOfWorkId}", Id);
                
                // If only one exception, unwrap it; otherwise wrap in aggregate
                if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }
                throw new AggregateException("Failed to rollback unit of work", exceptions);
            }

            _transactionsStarted = false;
        }

        private void HandleDisposalCleanup()
        {
            // If not completed and not marked to skip commit, log warning
            // We cannot call async RollbackAsync from synchronous Dispose
            // Users should explicitly call CommitAsync() or RollbackAsync() before disposal
            if (!_completed && !_skipCommit && Parent == null)
            {
                _logger.LogWarning(
                    "UnitOfWork {UnitOfWorkId} disposed without being completed. " +
                    "Transactions may not have been committed or rolled back. " +
                    "Always explicitly call CommitAsync() or RollbackAsync() before disposal.",
                    Id);
                
                // Mark as completed to prevent further operations
                MarkAsCompleted();
            }
            else if (_skipCommit && !_completed)
            {
                // Mark as completed if skip commit was set but CommitAsync wasn't called
                MarkAsCompleted();
                _logger.LogDebug("Marked UnitOfWork {UnitOfWorkId} as completed during disposal", Id);
            }
        }

        private void DisposeAllResources()
        {
            foreach (var resource in _resources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispose resource in UnitOfWork {UnitOfWorkId}", Id);
                }
            }
        }

        #endregion
    }
}
