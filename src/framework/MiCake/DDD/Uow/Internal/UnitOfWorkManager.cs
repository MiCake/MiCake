using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiCake.DDD.Uow.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Implementation of Unit of Work Manager with ambient frames, shared nested units of work,
    /// and isolated requiresNew execution scopes.
    /// The manager owns ambient frame bookkeeping; the caller or boundary owns the returned unit of work.
    /// </summary>
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AmbientUnitOfWorkAccessor _ambientAccessor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UnitOfWorkManager> _logger;
        private bool _disposed;

        public IUnitOfWork? Current => _ambientAccessor.Current?.UnitOfWork;

        public UnitOfWorkManager(
            IServiceProvider serviceProvider,
            AmbientUnitOfWorkAccessor ambientAccessor,
            IServiceScopeFactory scopeFactory,
            ILogger<UnitOfWorkManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _ambientAccessor = ambientAccessor ?? throw new ArgumentNullException(nameof(ambientAccessor));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Begins a unit of work. Ambient frame writes MUST happen in this synchronous segment so they
        /// land in the caller's execution context (async method bodies run on an execution-context copy).
        /// </summary>
        public Task<IUnitOfWork> BeginAsync(
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            options ??= UnitOfWorkOptions.Default;

            var currentFrame = _ambientAccessor.Current;

            // Shared nested unit of work: inherits the parent's isolation and read-only intent and
            // delegates all physical work to the root.
            if (currentFrame != null &&
                !currentFrame.UnitOfWork.IsDisposed &&
                !currentFrame.UnitOfWork.IsCompleted)
            {
                var nestedOptions = new UnitOfWorkOptions
                {
                    IsReadOnly = currentFrame.UnitOfWork.IsReadOnly,
                    IsolationLevel = currentFrame.UnitOfWork.IsolationLevel,
                    InitializationMode = options.InitializationMode
                };

                var token = UnitOfWorkFrameToken.New();
                var nestedUow = new UnitOfWork(
                    _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>(),
                    nestedOptions,
                    parent: currentFrame.UnitOfWork,
                    _ambientAccessor,
                    token,
                    currentFrame.ServiceProvider);

                _ambientAccessor.Push(new UnitOfWorkFrame(token, nestedUow, currentFrame.ServiceProvider, currentFrame));
                _logger.LogDebug("Created nested UnitOfWork {UnitOfWorkId} under parent {ParentId}",
                    nestedUow.Id, currentFrame.UnitOfWork.Id);
                return Task.FromResult<IUnitOfWork>(nestedUow);
            }

            var rootToken = UnitOfWorkFrameToken.New();
            var unitOfWork = new UnitOfWork(
                _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>(),
                options,
                parent: null,
                _ambientAccessor,
                rootToken,
                _serviceProvider);

            _ambientAccessor.Push(new UnitOfWorkFrame(rootToken, unitOfWork, _serviceProvider, currentFrame));
            _logger.LogDebug("Created root UnitOfWork {UnitOfWorkId}", unitOfWork.Id);

            return InitializeUnitOfWorkAsync(unitOfWork, _serviceProvider, options, cancellationToken);
        }

        public Task ExecuteRequiresNewAsync(
            Func<IServiceProvider, CancellationToken, Task> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(operation);

            return ExecuteRequiresNewCoreAsync(
                async (provider, ct) =>
                {
                    await operation(provider, ct).ConfigureAwait(false);
                    return true;
                },
                options,
                cancellationToken);
        }

        public Task<TResult> ExecuteRequiresNewAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(operation);

            return ExecuteRequiresNewCoreAsync(operation, options, cancellationToken);
        }

        private async Task<TResult> ExecuteRequiresNewCoreAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options,
            CancellationToken cancellationToken)
        {
            var outerFrame = _ambientAccessor.Current;
            if (outerFrame == null)
            {
                throw new InvalidOperationException(
                    "ExecuteRequiresNewAsync requires an active outer unit of work.");
            }

            options ??= UnitOfWorkOptions.Default;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var innerProvider = scope.ServiceProvider;

            var token = UnitOfWorkFrameToken.New();
            var innerUow = new UnitOfWork(
                innerProvider.GetRequiredService<ILogger<UnitOfWork>>(),
                options,
                parent: null,
                _ambientAccessor,
                token,
                innerProvider);

            _ambientAccessor.Push(new UnitOfWorkFrame(token, innerUow, innerProvider, outerFrame));
            _logger.LogDebug("Started isolated requiresNew UnitOfWork {UnitOfWorkId} under outer {OuterId}",
                innerUow.Id, outerFrame.UnitOfWork.Id);

            TResult result = default!;
            ExceptionDispatchInfo? primaryEDI = null;
            var rollbackFailures = new List<Exception>();
            Exception? cleanupFailure = null;

            try
            {
                // The inner UoW goes through the same initialization pipeline as BeginAsync
                // (lifecycle hooks and immediate activation) using the isolated scope's provider.
                await InitializeUnitOfWorkAsync(innerUow, innerProvider, options, cancellationToken).ConfigureAwait(false);

                result = await operation(innerProvider, cancellationToken).ConfigureAwait(false);
                await innerUow.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PartialUnitOfWorkCommitException ex)
            {
                // Partial commit is a terminal state: no overall rollback is attempted and no
                // rolled-back event may be raised; the original exception propagates as-is.
                primaryEDI = ExceptionDispatchInfo.Capture(ex);
            }
            catch (Exception ex)
            {
                primaryEDI = ExceptionDispatchInfo.Capture(ex);
                if (!innerUow.IsCompleted)
                {
                    try
                    {
                        // Cleanup rollback must not be cancelled by the operation's token.
                        await innerUow.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (UnitOfWorkBoundaryException rollbackEx)
                    {
                        rollbackFailures.AddRange(rollbackEx.RollbackExceptions);
                    }
                    catch (Exception rollbackEx)
                    {
                        rollbackFailures.Add(rollbackEx);
                    }
                }
            }
            finally
            {
                // Token-based compare-and-pop restores the outer frame on every exit path.
                _ambientAccessor.Pop(token);
                try
                {
                    await innerUow.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure = ex;
                    _logger.LogError(ex, "Failed to dispose requiresNew UnitOfWork {UnitOfWorkId}", innerUow.Id);
                }

                _logger.LogDebug("Restored ambient frame after requiresNew UnitOfWork {UnitOfWorkId}", innerUow.Id);
            }

            if (cleanupFailure != null)
            {
                throw new UnitOfWorkBoundaryException(
                    "The requiresNew operation completed but cleanup of the inner unit of work failed.",
                    primaryEDI?.SourceException,
                    rollbackFailures.Count > 0 ? rollbackFailures : null,
                    [cleanupFailure]);
            }

            if (rollbackFailures.Count > 0)
            {
                throw new UnitOfWorkBoundaryException(
                    "The requiresNew operation failed and rollback of eligible resources also failed.",
                    primaryEDI?.SourceException,
                    rollbackFailures);
            }

            primaryEDI?.Throw();
            return result;
        }

        private async Task<IUnitOfWork> InitializeUnitOfWorkAsync(
            UnitOfWork unitOfWork,
            IServiceProvider provider,
            UnitOfWorkOptions options,
            CancellationToken cancellationToken)
        {
            // Get hooks applicable to the initialization mode; hooks are resolved from the provider
            // that owns the unit of work (root provider for BeginAsync, isolated scope for requiresNew).
            var hooks = provider.GetServices<IUnitOfWorkLifetimeHook>()
                .Where(h => h.ApplicableMode == null || h.ApplicableMode == options.InitializationMode);

            foreach (var hook in hooks)
            {
                try
                {
                    await hook.OnUnitOfWorkCreatedAsync(unitOfWork, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lifecycle hook {HookType} failed for UnitOfWork {UnitOfWorkId}; disposing",
                        hook.GetType().Name, unitOfWork.Id);
                    await unitOfWork.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }

            if (options.InitializationMode == TransactionInitializationMode.Immediate)
            {
                // Immediately activate all registered resources
                try
                {
                    await unitOfWork.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Immediately activated resources for UnitOfWork {UnitOfWorkId}", unitOfWork.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate resources during immediate initialization for UnitOfWork {UnitOfWorkId}",
                        unitOfWork.Id);
                    await unitOfWork.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }

            return unitOfWork;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // The manager does not own the ambient unit of work; callers or request boundaries dispose it.
                _logger.LogDebug("Disposing UnitOfWorkManager");
            }

            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
