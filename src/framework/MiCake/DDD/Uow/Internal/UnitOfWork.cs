using Microsoft.Extensions.Logging;
using MiCake.DDD.Uow.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Implementation of Unit of Work with explicit transactions, shared nested units of work,
    /// deterministic registration-order flush, best-effort multi-resource commit, savepoint coverage,
    /// and asynchronous disposal. The unit of work is the sole persistence owner.
    /// </summary>
    internal class UnitOfWork : IUnitOfWork, IUnitOfWorkInternal
    {
        #region Fields

        private readonly List<IUnitOfWorkResource> _resources = [];
        private readonly Dictionary<UnitOfWorkResourceId, UnitOfWorkResourceCommitState> _commitStates = [];
        private readonly Dictionary<UnitOfWorkResourceId, UnitOfWorkResourceCommitState> _rollbackStates = [];
        private readonly HashSet<UnitOfWorkResourceId> _activatedResources = [];
        private readonly Dictionary<string, HashSet<UnitOfWorkResourceId>> _savepointCoverage = [];
        private readonly Dictionary<string, HashSet<UnitOfWorkResourceId>> _savepointAttempted = [];
        private readonly ILogger<UnitOfWork> _logger;
        private readonly UnitOfWorkOptions _options;
        private readonly AmbientUnitOfWorkAccessor? _ambientAccessor;
        private readonly UnitOfWorkFrameToken? _frameToken;
        private readonly Lock _lock = new();

        private bool _disposed;
        private bool _completed;
        private bool _transactionsStarted;
        private bool _shouldRollback;
        private bool _hasPartialCommit;

        #endregion

        #region Properties

        public Guid Id { get; }
        public bool IsDisposed => _disposed;
        public bool IsCompleted => _completed;
        public bool IsReadOnly => _options.IsReadOnly;

        /// <summary>
        /// True when any registered resource currently holds an active transaction.
        /// Reflects the real per-resource state even when activation failed partway through.
        /// </summary>
        public bool HasActiveTransactions =>
            _transactionsStarted || _resources.Any(r => r.HasActiveTransaction);

        public IsolationLevel? IsolationLevel => _options.IsolationLevel;
        public IUnitOfWork? Parent { get; }
        public bool IsNested => Parent != null;

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
            IUnitOfWork? parent = null,
            AmbientUnitOfWorkAccessor? ambientAccessor = null,
            UnitOfWorkFrameToken? frameToken = null)
        {
            Id = Guid.NewGuid();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? UnitOfWorkOptions.Default;
            Parent = parent;
            _ambientAccessor = ambientAccessor;
            _frameToken = frameToken;

            _logger.LogDebug(
                "Created UnitOfWork {UnitOfWorkId} (Nested: {Nested}, IsolationLevel: {IsolationLevel}, InitMode: {InitMode}, ReadOnly: {ReadOnly})",
                Id, Parent != null, _options.IsolationLevel, _options.InitializationMode, _options.IsReadOnly);
        }

        #region Public Methods

        public void RegisterResource(IUnitOfWorkResource resource)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot register resource after unit of work is completed");
            ArgumentNullException.ThrowIfNull(resource);

            // Nested UoWs delegate resource ownership to the root.
            if (Parent is IUnitOfWorkInternal parentInternal)
            {
                parentInternal.RegisterResource(resource);
                return;
            }

            lock (_lock)
            {
                if (_commitStates.ContainsKey(resource.Id))
                {
                    _logger.LogDebug("Resource {ResourceId} already registered with UnitOfWork {UnitOfWorkId}",
                        resource.Id, Id);
                    return;
                }

                try
                {
                    resource.Prepare(new UnitOfWorkResourceContext(Id, _options.IsolationLevel, _options.IsReadOnly));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to prepare resource {ResourceId} for UnitOfWork {UnitOfWorkId}",
                        resource.Id, Id);
                    throw;
                }

                _resources.Add(resource);
                _commitStates[resource.Id] = UnitOfWorkResourceCommitState.Pending;
                _logger.LogDebug("Resource {ResourceId} ({ResourceType}) registered with UnitOfWork {UnitOfWorkId}",
                    resource.Id, resource.ResourceType, Id);
            }
        }

        /// <summary>
        /// Ensures every registered resource has an active transaction (idempotent per resource).
        /// Rejected on read-only units of work. Nested units of work delegate to the root.
        /// Resources registered after an earlier activation pass are still activated here
        /// (the global short-circuit was removed because it let late resources flush untransacted).
        /// </summary>
        public async Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Nested UoW: delegate to parent
            if (Parent != null)
            {
                if (Parent is IUnitOfWorkInternal parentInternal)
                {
                    await parentInternal.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            if (_resources.Count == 0)
            {
                _logger.LogDebug("Skipping activation for UnitOfWork {UnitOfWorkId} (no resources registered)", Id);
                return;
            }

            if (_options.IsReadOnly)
            {
                throw new InvalidOperationException($"Unit of work {Id} is read-only and cannot activate transactions.");
            }

            foreach (var resource in _resources)
            {
                // Idempotent per resource; only unactivated resources call EnsureTransactionAsync.
                if (_activatedResources.Contains(resource.Id))
                {
                    continue;
                }

                try
                {
                    await resource.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
                    _activatedResources.Add(resource.Id);
                    _logger.LogDebug("Activated resource {ResourceId} ({ResourceType}) in UnitOfWork {UnitOfWorkId}",
                        resource.Id, resource.ResourceType, Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate resource {ResourceId} in UnitOfWork {UnitOfWorkId}; marking rollback-only",
                        resource.Id, Id);
                    _shouldRollback = true;
                    throw;
                }
            }

            _transactionsStarted = true;
            _logger.LogDebug("Successfully activated all resources for UnitOfWork {UnitOfWorkId}", Id);
        }

        public async Task<int> FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot flush after unit of work is completed");

            // Nested UoW: delegate to parent
            if (Parent != null)
            {
                return await Parent.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_options.IsReadOnly)
            {
                throw new InvalidOperationException($"Unit of work {Id} is read-only and rejects resource flush.");
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            if (_resources.Count == 0)
            {
                return 0;
            }

            _logger.LogDebug("Flushing {Count} resources for UnitOfWork {UnitOfWorkId}", _resources.Count, Id);

            var totalAffected = 0;
            foreach (var resource in _resources)
            {
                try
                {
                    totalAffected += await resource.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Flushed resource {ResourceId} ({ResourceType}) in UnitOfWork {UnitOfWorkId}",
                        resource.Id, resource.ResourceType, Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to flush resource {ResourceId} in UnitOfWork {UnitOfWorkId}; marking rollback-only",
                        resource.Id, Id);
                    _shouldRollback = true;
                    throw;
                }
            }

            return totalAffected;
        }

        /// <summary>
        /// Commits the unit of work. Completion and ambient-frame changes that must be visible to the
        /// caller happen in this synchronous segment; physical commit runs in <see cref="CommitCoreAsync"/>.
        /// </summary>
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Unit of work has already been completed");

            // Shared nested commit performs no physical commit, raises no transaction events,
            // and completes in the caller's execution context.
            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} completed successfully", Id);
                MarkAsCompleted();
                return Task.CompletedTask;
            }

            // Read-only units of work and units of work without resources have nothing physical to commit.
            if (_options.IsReadOnly || _resources.Count == 0)
            {
                _logger.LogDebug("Skipping physical commit for UnitOfWork {UnitOfWorkId} (ReadOnly: {ReadOnly}, Resources: {Count})",
                    Id, _options.IsReadOnly, _resources.Count);
                MarkAsCompleted();
                return Task.CompletedTask;
            }

            return CommitCoreAsync(cancellationToken);
        }

        private async Task CommitCoreAsync(CancellationToken cancellationToken)
        {
            if (_shouldRollback)
            {
                _logger.LogWarning("UnitOfWork {UnitOfWorkId} is marked rollback-only; rolling back", Id);
                var rollbackOnlyFailures = await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackOnlyFailures.Count > 0)
                {
                    throw UnitOfWorkBoundaryException.ForRollbackFailureOnly(rollbackOnlyFailures);
                }
                throw new InvalidOperationException("Cannot commit: the unit of work is marked rollback-only.");
            }

            // OnCommitting is raised once before the first physical commit; a handler failure aborts the commit.
            try
            {
                RaiseEvent(OnCommitting, new UnitOfWorkEventArgs(Id, Parent != null), nameof(OnCommitting), throwOnFailure: true);
            }
            catch (Exception ex)
            {
                var rollbackFailures = await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackFailures.Count > 0)
                {
                    throw UnitOfWorkBoundaryException.ForRollbackFailure(ex, rollbackFailures);
                }
                throw;
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            // Flush every resource before committing; a flush failure rolls back eligible resources.
            foreach (var resource in _resources)
            {
                try
                {
                    await resource.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to flush resource {ResourceId} during commit of UnitOfWork {UnitOfWorkId}", resource.Id, Id);
                    var rollbackFailures = await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                    if (rollbackFailures.Count > 0)
                    {
                        throw UnitOfWorkBoundaryException.ForRollbackFailure(ex, rollbackFailures);
                    }
                    throw;
                }
            }

            // Commit resources in deterministic registration order with structured outcome tracking.
            // CommitState and RollbackState are tracked separately so a commit-failed resource is
            // never conflated with a resource that was never committed (ADR-007 diagnostic contract).
            var commitFailures = new List<Exception>();

            foreach (var resource in _resources)
            {
                try
                {
                    await resource.CommitAsync(cancellationToken).ConfigureAwait(false);
                    _commitStates[resource.Id] = UnitOfWorkResourceCommitState.Committed;
                }
                catch (Exception ex)
                {
                    _commitStates[resource.Id] = UnitOfWorkResourceCommitState.Failed;
                    commitFailures.Add(ex);
                    _logger.LogError(ex, "Failed to commit resource {ResourceId} in UnitOfWork {UnitOfWorkId}", resource.Id, Id);
                    break;
                }
            }

            if (commitFailures.Count > 0)
            {
                var rollbackFailures = await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
                var finalOutcomes = BuildOutcomes();

                if (!finalOutcomes.Any(o => o.CommitState == UnitOfWorkResourceCommitState.Committed))
                {
                    // No resource committed: this is a plain commit failure, not a partial commit.
                    if (rollbackFailures.Count > 0)
                    {
                        throw UnitOfWorkBoundaryException.ForRollbackFailure(commitFailures[0], rollbackFailures);
                    }

                    ExceptionDispatchInfo.Capture(commitFailures[0]).Throw();
                }

                // Partial commit: the UoW is not rolled back as a whole and cannot be later
                // reported as such; it becomes a terminal state until the boundary disposes it.
                _hasPartialCommit = true;
                throw new PartialUnitOfWorkCommitException(
                    "Unit of work commit partially failed; inspect the outcome for per-resource state. The unit of work is not rolled back as a whole.",
                    new UnitOfWorkCommitOutcome(Id, finalOutcomes),
                    commitFailures,
                    rollbackFailures);
            }

            MarkAsCompleted();
            _transactionsStarted = false;
            _logger.LogDebug("Successfully committed UnitOfWork {UnitOfWorkId}", Id);

            // Raise OnCommitted event
            RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null), nameof(OnCommitted));
        }

        /// <summary>
        /// Rolls back the unit of work. Shared nested completion and ambient-frame changes that must be
        /// visible to the caller happen in this synchronous segment; physical rollback runs in
        /// <see cref="RollbackCoreAsync"/>.
        /// </summary>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCompleted("Cannot roll back a completed unit of work");

            // A partial commit is a terminal state: some resources are durable, so the UoW
            // cannot be reported as rolled back as a whole. Only the boundary may dispose it.
            if (_hasPartialCommit)
            {
                throw new InvalidOperationException(
                    $"Unit of work {Id} is in a partial-commit state and cannot be rolled back as a whole; dispose it to release resources.");
            }

            // Shared nested rollback marks the root rollback-only, raises no physical transaction events,
            // and completes in the caller's execution context.
            if (Parent != null)
            {
                if (Parent is UnitOfWork parentUow)
                {
                    parentUow.MarkRollbackOnly();
                }

                MarkAsCompleted();
                _logger.LogWarning("Nested UnitOfWork {UnitOfWorkId} requested rollback of parent {ParentId}",
                    Id, Parent.Id);
                return Task.CompletedTask;
            }

            if (_options.IsReadOnly || _resources.Count == 0)
            {
                MarkAsCompleted();
                return Task.CompletedTask;
            }

            return RollbackCoreAsync(cancellationToken);
        }

        private async Task RollbackCoreAsync(CancellationToken cancellationToken)
        {
            var failures = await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);

            if (failures.Count > 0)
            {
                _logger.LogError("Rollback of UnitOfWork {UnitOfWorkId} failed for {Count} resources", Id, failures.Count);
                throw UnitOfWorkBoundaryException.ForRollbackFailureOnly(failures);
            }

            MarkAsCompleted();
            _logger.LogDebug("Successfully rolled back UnitOfWork {UnitOfWorkId}", Id);

            // Raise OnRolledBack event
            RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null), nameof(OnRolledBack));
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

            if (_options.IsReadOnly)
            {
                throw new InvalidOperationException($"Unit of work {Id} is read-only and cannot create savepoints.");
            }

            await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

            if (!_transactionsStarted || _resources.Count == 0)
            {
                throw new InvalidOperationException("Cannot create savepoint: no active transaction");
            }

            _logger.LogDebug("Creating savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            // Validate before changing state: every registered resource must support savepoints.
            foreach (var resource in _resources)
            {
                if (!resource.SupportsSavepoints)
                {
                    throw new NotSupportedException($"Resource {resource.Id} ({resource.ResourceType}) does not support savepoints.");
                }
            }

            var exceptions = new List<Exception>();
            var createdFor = new List<IUnitOfWorkResource>();

            // All resources present at creation time were attempted; this distinguishes them from
            // resources registered later (which are rejected by coverage validation).
            _savepointAttempted[name] = new HashSet<UnitOfWorkResourceId>(_resources.Select(r => r.Id));

            foreach (var resource in _resources)
            {
                try
                {
                    await resource.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                    createdFor.Add(resource);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to create savepoint '{SavepointName}' for resource {ResourceId}",
                        name, resource.Id);
                }
            }

            // Record partial coverage so savepoints that were actually created on some resources
            // remain usable; resources that failed are simply not covered.
            if (createdFor.Count > 0)
            {
                _savepointCoverage[name] = new HashSet<UnitOfWorkResourceId>(createdFor.Select(r => r.Id));
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
            ThrowIfCompleted("Cannot roll back to savepoint after unit of work is completed");
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (Parent != null)
            {
                _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} delegating savepoint rollback to parent", Id);
                await Parent.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                return;
            }

            ValidateSavepointCoverage(name);

            _logger.LogDebug("Rolling back to savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            var exceptions = new List<Exception>();
            foreach (var resource in GetCoveredResources(name))
            {
                try
                {
                    await resource.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to roll back to savepoint '{SavepointName}' for resource {ResourceId}",
                        name, resource.Id);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to roll back to savepoint '{name}'", exceptions);
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

            ValidateSavepointCoverage(name);

            _logger.LogDebug("Releasing savepoint '{SavepointName}' in UnitOfWork {UnitOfWorkId}", name, Id);

            var exceptions = new List<Exception>();
            foreach (var resource in GetCoveredResources(name))
            {
                try
                {
                    await resource.ReleaseSavepointAsync(name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to release savepoint '{SavepointName}' for resource {ResourceId}",
                        name, resource.Id);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to release savepoint '{name}'", exceptions);
            }

            _savepointCoverage.Remove(name);
            _savepointAttempted.Remove(name);
        }

        #endregion

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _logger.LogDebug("Disposing UnitOfWork {UnitOfWorkId} (Completed: {Completed}, Nested: {Nested})",
                    Id, _completed, Parent != null);

                if (!_completed && Parent == null)
                {
                    _logger.LogWarning(
                        "UnitOfWork {UnitOfWorkId} disposed without being completed. " +
                        "Transactions may not have been committed or rolled back. " +
                        "Always explicitly call CommitAsync() or RollbackAsync() before disposal.",
                        Id);

                    MarkAsCompleted();
                }

                // Only dispose resources if this is root UoW
                if (Parent == null)
                {
                    DisposeResources();
                    _resources.Clear();
                    _commitStates.Clear();
                    _rollbackStates.Clear();
                }

                PopFrame();
                _logger.LogDebug("Disposed UnitOfWork {UnitOfWorkId}", Id);
            }

            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            List<Exception>? rollbackFailures = null;
            List<Exception>? cleanupFailures = null;

            if (Parent == null)
            {
                if (!_completed && !_hasPartialCommit && (_transactionsStarted || _resources.Any(r => r.HasActiveTransaction)))
                {
                    // Best-effort rollback of still-active transactions on asynchronous disposal.
                    // Failures are collected and surfaced instead of being swallowed (boundary contract).
                    _logger.LogWarning(
                        "UnitOfWork {UnitOfWorkId} disposed without being completed; rolling back active transactions best-effort",
                        Id);
                    rollbackFailures = await RollbackInternalAsync(CancellationToken.None).ConfigureAwait(false);
                    if (rollbackFailures.Count == 0)
                    {
                        MarkAsCompleted();
                    }
                }

                cleanupFailures = await DisposeResourcesAsync().ConfigureAwait(false);
                _resources.Clear();
                _commitStates.Clear();
                _rollbackStates.Clear();
            }

            PopFrame();
            _disposed = true;
            GC.SuppressFinalize(this);

            if (rollbackFailures is { Count: > 0 } || cleanupFailures is { Count: > 0 })
            {
                throw new UnitOfWorkBoundaryException(
                    "Unit of work disposal failed while rolling back active transactions or disposing resources.",
                    rollbackExceptions: rollbackFailures,
                    cleanupExceptions: cleanupFailures);
            }
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

        private void MarkAsCompleted()
        {
            _completed = true;
            PopFrame();
        }

        /// <summary>
        /// Marks the root unit of work as rollback-only. Called by a nested unit of work on rollback
        /// or by framework components when a save operation cannot proceed safely.
        /// </summary>
        public void MarkRollbackOnly()
        {
            lock (_lock)
            {
                _shouldRollback = true;
            }
        }

        private void PopFrame()
        {
            if (_ambientAccessor != null && _frameToken.HasValue)
            {
                _ambientAccessor.Pop(_frameToken.Value);
            }
        }

        private void RaiseEvent(EventHandler<UnitOfWorkEventArgs>? eventHandler, UnitOfWorkEventArgs args, string eventName, bool throwOnFailure = false)
        {
            if (eventHandler == null)
                return;

            try
            {
                eventHandler.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising {EventName} event in UnitOfWork {UnitOfWorkId}", eventName, Id);
                if (throwOnFailure)
                    throw;
            }
        }

        /// <summary>
        /// Rolls back all eligible (not committed, not already rolled back) resources in registration order.
        /// Raises <see cref="OnRollingBack"/> once before the rollback attempts. Never throws;
        /// all failures are returned so callers can combine them with the primary failure.
        /// </summary>
        private async Task<List<Exception>> RollbackInternalAsync(CancellationToken cancellationToken)
        {
            var failures = new List<Exception>();

            var eligible = _resources
                .Where(r => _commitStates.TryGetValue(r.Id, out var commitState) &&
                            commitState != UnitOfWorkResourceCommitState.Committed &&
                            !_rollbackStates.ContainsKey(r.Id))
                .ToList();

            if (eligible.Count == 0)
            {
                return failures;
            }

            try
            {
                RaiseEvent(OnRollingBack, new UnitOfWorkEventArgs(Id, Parent != null), nameof(OnRollingBack), throwOnFailure: true);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
                _logger.LogError(ex, "OnRollingBack handler failed in UnitOfWork {UnitOfWorkId}; continuing rollback", Id);
            }

            foreach (var resource in eligible)
            {
                try
                {
                    await resource.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    _rollbackStates[resource.Id] = UnitOfWorkResourceCommitState.RolledBack;
                    _logger.LogDebug("Rolled back resource {ResourceId} in UnitOfWork {UnitOfWorkId}", resource.Id, Id);
                }
                catch (Exception ex)
                {
                    _rollbackStates[resource.Id] = UnitOfWorkResourceCommitState.RollbackFailed;
                    failures.Add(ex);
                    _logger.LogError(ex, "Failed to roll back resource {ResourceId} in UnitOfWork {UnitOfWorkId}", resource.Id, Id);
                }
            }

            if (failures.Count == 0)
            {
                _transactionsStarted = false;
            }

            return failures;
        }

        /// <summary>
        /// Builds the structured per-resource outcome. CommitState reflects the commit attempt and is
        /// never overwritten by a later rollback; RollbackState is null when no rollback was attempted.
        /// </summary>
        private List<UnitOfWorkResourceOutcome> BuildOutcomes()
            => _resources
                .Select(r => new UnitOfWorkResourceOutcome(
                    r.Id,
                    r.ResourceType,
                    _commitStates.TryGetValue(r.Id, out var commitState)
                        ? commitState
                        : UnitOfWorkResourceCommitState.Pending,
                    _rollbackStates.TryGetValue(r.Id, out var rollbackState)
                        ? rollbackState
                        : null))
                .ToList();

        private void ValidateSavepointCoverage(string name)
        {
            if (!_savepointCoverage.TryGetValue(name, out _))
            {
                throw new InvalidOperationException($"Savepoint '{name}' does not exist in unit of work {Id}.");
            }

            // Resources registered after the savepoint was created are not covered and must be
            // rejected before any state change (R8). Resources that were present at creation time
            // but failed to create the savepoint are simply skipped, not treated as late registrations.
            var attempted = _savepointAttempted[name];
            foreach (var resource in _resources)
            {
                if (!attempted.Contains(resource.Id))
                {
                    throw new InvalidOperationException(
                        $"Savepoint '{name}' does not cover resource {resource.Id} ({resource.ResourceType}) registered after the savepoint was created.");
                }

                if (!resource.SupportsSavepoints)
                {
                    throw new NotSupportedException($"Resource {resource.Id} ({resource.ResourceType}) does not support savepoints.");
                }
            }
        }

        /// <summary>
        /// Returns the resources covered by the savepoint, in registration order.
        /// Resources that failed to create the savepoint are excluded.
        /// </summary>
        private List<IUnitOfWorkResource> GetCoveredResources(string name)
        {
            var covered = _savepointCoverage[name];
            return _resources.Where(r => covered.Contains(r.Id)).ToList();
        }

        private void DisposeResources()
        {
            for (var i = _resources.Count - 1; i >= 0; i--)
            {
                try
                {
                    _resources[i].Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispose resource {ResourceId} in UnitOfWork {UnitOfWorkId}", _resources[i].Id, Id);
                }
            }
        }

        private async Task<List<Exception>> DisposeResourcesAsync()
        {
            var failures = new List<Exception>();
            for (var i = _resources.Count - 1; i >= 0; i--)
            {
                try
                {
                    await _resources[i].DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                    _logger.LogError(ex, "Failed to dispose resource {ResourceId} in UnitOfWork {UnitOfWorkId}", _resources[i].Id, Id);
                }
            }

            return failures;
        }

        #endregion
    }
}
