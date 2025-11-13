using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Implementation of Unit of Work Manager with support for nested transactions using AsyncLocal
    /// </summary>
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private readonly AsyncLocal<IUnitOfWork?> _current = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UnitOfWorkManager> _logger;
        private bool _disposed = false;

        public IUnitOfWork? Current => _current.Value;

        public UnitOfWorkManager(IServiceProvider serviceProvider, ILogger<UnitOfWorkManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IUnitOfWork> BeginAsync(bool requiresNew = false, CancellationToken cancellationToken = default)
        {
            return BeginAsync(UnitOfWorkOptions.Default, requiresNew, cancellationToken);
        }

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, bool requiresNew = false, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(options);

            // If we already have a UoW and don't require a new one, return nested UoW
            if (!requiresNew && _current.Value != null && !_current.Value.IsDisposed)
            {
                var parentUowWrapper = _current.Value;  // This is the wrapper!

                _logger.LogDebug("Creating nested UnitOfWork under parent {ParentId}", parentUowWrapper.Id);

                // Get the inner UnitOfWork from parent (for internal linking)
                var parentInner = GetInnerUnitOfWork(parentUowWrapper);

                // Create nested UoW (inherits parent's options for isolation level)
                // Nested UoW is read-only and doesn't manage its own transactions
                var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
                var nestedOptions = new UnitOfWorkOptions
                {
                    IsolationLevel = parentUowWrapper.IsolationLevel,
                    IsReadOnly = true,  // Nested UoW doesn't manage transactions
                    InitializationMode = options.InitializationMode
                };

                var nestedUow = new UnitOfWork(logger, nestedOptions, parentInner);

                // Return wrapper that stores parent wrapper reference and updates AsyncLocal
                return Task.FromResult<IUnitOfWork>(new NestedUnitOfWorkWrapper(nestedUow, parentUowWrapper, _current, _logger));
            }

            var uowLogger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            var unitOfWork = new UnitOfWork(uowLogger, options, parent: null);

            _logger.LogDebug("Created new root UnitOfWork {UnitOfWorkId}", unitOfWork.Id);

            // Create wrapper and set AsyncLocal BEFORE any async operations
            var wrapper = new RootUnitOfWorkWrapper(unitOfWork, _current, _logger);

            return InitializeUnitOfWorkAsync(unitOfWork, wrapper, options, cancellationToken);
        }

        private async Task<IUnitOfWork> InitializeUnitOfWorkAsync(
            UnitOfWork unitOfWork,
            IUnitOfWork wrapper,
            UnitOfWorkOptions options,
            CancellationToken cancellationToken)
        {
            // Get hooks applicable to the initialization mode
            var hooks = _serviceProvider.GetServices<IUnitOfWorkLifecycleHook>()
                .Where(h => h.ApplicableMode == null || h.ApplicableMode == options.InitializationMode);
            
            foreach (var hook in hooks)
            {
                try
                {
                    await hook.OnUnitOfWorkCreatedAsync(unitOfWork, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling lifecycle hook {HookType} for UnitOfWork {UnitOfWorkId}",
                        hook.GetType().Name, unitOfWork.Id);
                    wrapper.Dispose();
                    throw;
                }
            }

            if (options.InitializationMode == TransactionInitializationMode.Immediate)
            {
                // Immediately activate all registered resources
                if (unitOfWork is IUnitOfWorkInternal internalUow)
                {
                    try
                    {
                        await internalUow.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
                        _logger.LogDebug("Immediately activated resources for UnitOfWork {UnitOfWorkId}", unitOfWork.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to activate resources during immediate initialization for UnitOfWork {UnitOfWorkId}",
                            unitOfWork.Id);
                        wrapper.Dispose();
                        throw;
                    }
                }
            }

            return wrapper;
        }

        /// <summary>
        /// Extracts the inner UnitOfWork from a wrapper
        /// </summary>
        private static UnitOfWork GetInnerUnitOfWork(IUnitOfWork wrapper)
        {
            return wrapper switch
            {
                RootUnitOfWorkWrapper root => root.Inner,
                NestedUnitOfWorkWrapper nested => nested.Inner,
                UnitOfWork uow => uow,
                _ => throw new InvalidOperationException($"Unknown wrapper type: {wrapper.GetType()}")
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _logger.LogDebug("Disposing UnitOfWorkManager");

            // Dispose current UOW if exists
            _current.Value?.Dispose();
            _current.Value = null;

            _disposed = true;
        }

        /// <summary>
        /// Wrapper for root UnitOfWork to handle cleanup of AsyncLocal context
        /// </summary>
        private class RootUnitOfWorkWrapper : UnitOfWorkWrapperBase
        {
            private readonly AsyncLocal<IUnitOfWork?> _currentRef;

            public RootUnitOfWorkWrapper(UnitOfWork inner, AsyncLocal<IUnitOfWork?> currentRef, ILogger logger)
                : base(inner, logger)
            {
                _currentRef = currentRef;
                
                // CRITICAL: Set AsyncLocal value in constructor, which executes in caller's context
                // Store THIS (the wrapper) as current, not the inner UnitOfWork
                // This ensures the value persists after BeginAsync returns
                _currentRef.Value = this;
            }

            public override void Dispose()
            {
                base.Dispose();
                
                // Clear current if this is still the current UoW
                if (_currentRef.Value == this)
                {
                    _logger.LogDebug("Clearing current UoW {UowId}", _inner.Id);
                    _currentRef.Value = null;
                }
            }
        }

        /// <summary>
        /// Wrapper for nested UnitOfWork (doesn't modify AsyncLocal)
        /// </summary>
        private class NestedUnitOfWorkWrapper : UnitOfWorkWrapperBase
        {
            private readonly IUnitOfWork _parentWrapper;
            private readonly AsyncLocal<IUnitOfWork?> _currentRef;

            public NestedUnitOfWorkWrapper(UnitOfWork inner, IUnitOfWork parentWrapper, AsyncLocal<IUnitOfWork?> currentRef, ILogger logger)
                : base(inner, logger)
            {
                _parentWrapper = parentWrapper;
                _currentRef = currentRef;
                // Update current to point to this nested UoW
                _currentRef.Value = this;
            }

            // Override Parent to return the wrapper instead of inner UoW
            public override IUnitOfWork? Parent => _parentWrapper;

            public override void Dispose()
            {
                base.Dispose();
                // Restore parent as current when this nested UoW is disposed
                if (_currentRef.Value == this)
                {
                    _currentRef.Value = _parentWrapper;
                }
            }
        }

        /// <summary>
        /// Base wrapper class for UnitOfWork
        /// </summary>
        private abstract class UnitOfWorkWrapperBase : IUnitOfWork
        {
            protected readonly UnitOfWork _inner;
            protected readonly ILogger _logger;

            protected UnitOfWorkWrapperBase(UnitOfWork inner, ILogger logger)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            /// <summary>
            /// Exposes the inner UnitOfWork for wrapper type checking
            /// </summary>
            internal UnitOfWork Inner => _inner;

            public Guid Id => _inner.Id;
            public bool IsDisposed => _inner.IsDisposed;
            public bool IsCompleted => _inner.IsCompleted;
            public bool HasActiveTransactions => _inner.HasActiveTransactions;
            public System.Data.IsolationLevel? IsolationLevel => _inner.IsolationLevel;
            public virtual IUnitOfWork? Parent => _inner.Parent;

            public event EventHandler<UnitOfWorkEventArgs>? OnCommitting
            {
                add => _inner.OnCommitting += value;
                remove => _inner.OnCommitting -= value;
            }

            public event EventHandler<UnitOfWorkEventArgs>? OnCommitted
            {
                add => _inner.OnCommitted += value;
                remove => _inner.OnCommitted -= value;
            }

            public event EventHandler<UnitOfWorkEventArgs>? OnRollingBack
            {
                add => _inner.OnRollingBack += value;
                remove => _inner.OnRollingBack -= value;
            }

            public event EventHandler<UnitOfWorkEventArgs>? OnRolledBack
            {
                add => _inner.OnRolledBack += value;
                remove => _inner.OnRolledBack -= value;
            }

            public async Task CommitAsync(CancellationToken cancellationToken = default)
            {
                await _inner.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            public async Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                await _inner.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            public void RegisterResource(IUnitOfWorkResource resource)
            {
                if (_inner is IUnitOfWorkInternal internalUow)
                {
                    internalUow.RegisterResource(resource);
                }
            }

            public Task MarkAsCompletedAsync(CancellationToken cancellationToken = default)
            {
                return _inner.MarkAsCompletedAsync(cancellationToken);
            }

            public async Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                return await _inner.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
            }

            public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                await _inner.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
            }

            public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                await _inner.ReleaseSavepointAsync(name, cancellationToken).ConfigureAwait(false);
            }

            public virtual void Dispose()
            {
                _inner.Dispose();
            }
        }
    }
}
