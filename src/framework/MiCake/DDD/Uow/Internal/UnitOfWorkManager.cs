using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private static readonly AsyncLocal<IUnitOfWork?> _current = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UnitOfWorkManager> _logger;
        private bool _disposed = false;

        public IUnitOfWork? Current => _current.Value;

        public UnitOfWorkManager(IServiceProvider serviceProvider, ILogger<UnitOfWorkManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IUnitOfWork Begin(bool requiresNew = false)
        {
            return Begin(UnitOfWorkOptions.Default, requiresNew);
        }

        public IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(options);

            // If we already have a UoW and don't require a new one, return nested UoW
            if (!requiresNew && _current.Value != null && !_current.Value.IsDisposed)
            {
                var parentUow = _current.Value;
                
                _logger.LogDebug("Creating nested UnitOfWork under parent {ParentId}", parentUow.Id);

                // Create nested UoW (inherits parent's options for isolation level)
                var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
                var nestedOptions = new UnitOfWorkOptions
                {
                    IsolationLevel = parentUow.IsolationLevel,
                    AutoBeginTransaction = false,  // Nested doesn't manage transactions
                    IsReadOnly = options.IsReadOnly,
                    InitializationMode = options.InitializationMode
                };
                
                var nestedUow = new UnitOfWork(logger, nestedOptions, parentUow);
                
                // Return wrapper that doesn't affect AsyncLocal
                return new NestedUnitOfWorkWrapper(nestedUow, _logger);
            }

            // Create a new root unit of work
            var uowLogger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            var unitOfWork = new UnitOfWork(uowLogger, options, parent: null);

            // Set as current
            _current.Value = unitOfWork;

            _logger.LogDebug("Created new root UnitOfWork {UnitOfWorkId}", unitOfWork.Id);

            // Call lifecycle hooks if configured for immediate initialization
            if (options.InitializationMode == TransactionInitializationMode.Immediate)
            {
                var hooks = _serviceProvider.GetServices<IUnitOfWorkLifecycleHook>();
                if (hooks.Any())
                {
                    foreach (var hook in hooks)
                    {
                        try
                        {
                            // Call hook synchronously to ensure initialization completes before returning
                            Task.Run(async () => await hook.OnUnitOfWorkCreatedAsync(unitOfWork, options, default).ConfigureAwait(false))
                                .GetAwaiter()
                                .GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling lifecycle hook {HookType} for UnitOfWork {UnitOfWorkId}",
                                hook.GetType().Name, unitOfWork.Id);
                            throw;
                        }
                    }
                }
            }

            // Return a wrapper that clears the current UOW when disposed
            return new RootUnitOfWorkWrapper(unitOfWork, () =>
            {
                if (_current.Value == unitOfWork)
                {
                    _current.Value = null;
                }
            }, _logger);
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
            private readonly Action _onDispose;

            public RootUnitOfWorkWrapper(UnitOfWork inner, Action onDispose, ILogger logger)
                : base(inner, logger)
            {
                _onDispose = onDispose;
            }

            public override void Dispose()
            {
                base.Dispose();
                _onDispose?.Invoke();
            }
        }

        /// <summary>
        /// Wrapper for nested UnitOfWork (doesn't modify AsyncLocal)
        /// </summary>
        private class NestedUnitOfWorkWrapper : UnitOfWorkWrapperBase
        {
            public NestedUnitOfWorkWrapper(UnitOfWork inner, ILogger logger)
                : base(inner, logger)
            {
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

            public Guid Id => _inner.Id;
            public bool IsDisposed => _inner.IsDisposed;
            public bool IsCompleted => _inner.IsCompleted;
            public bool HasActiveTransactions => _inner.HasActiveTransactions;
            public System.Data.IsolationLevel? IsolationLevel => _inner.IsolationLevel;
            public IUnitOfWork? Parent => _inner.Parent;

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
