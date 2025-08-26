using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Clean implementation of Unit of Work Manager using AsyncLocal for context
    /// </summary>
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private static readonly AsyncLocal<IUnitOfWork> _current = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UnitOfWorkManager> _logger;
        private bool _disposed = false;

        public IUnitOfWork Current => _current.Value;

        public UnitOfWorkManager(IServiceProvider serviceProvider, ILogger<UnitOfWorkManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IUnitOfWork Begin(bool requiresNew = false)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // If we already have a UOW and don't require a new one, return the current one
            if (!requiresNew && _current.Value != null && !_current.Value.IsDisposed)
            {
                _logger.LogDebug("Returning existing UnitOfWork {UnitOfWorkId}", _current.Value.Id);
                return _current.Value;
            }

            // Create a new unit of work
            var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            var unitOfWork = new UnitOfWork(logger);

            // Set as current
            _current.Value = unitOfWork;

            _logger.LogDebug("Created new UnitOfWork {UnitOfWorkId}", unitOfWork.Id);

            // Return a wrapper that clears the current UOW when disposed
            return new UnitOfWorkWrapper(unitOfWork, () =>
            {
                if (_current.Value == unitOfWork)
                {
                    _current.Value = null;
                }
            });
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
        /// Wrapper to handle cleanup of AsyncLocal context
        /// </summary>
        public class UnitOfWorkWrapper : IUnitOfWork
        {
            private readonly UnitOfWork _inner;
            private readonly Action _onDispose;

            public UnitOfWorkWrapper(UnitOfWork inner, Action onDispose)
            {
                _inner = inner;
                _onDispose = onDispose;
            }

            public Guid Id => _inner.Id;
            public bool IsDisposed => _inner.IsDisposed;
            public bool IsCompleted => _inner.IsCompleted;

            public bool HasActiveTransactions => throw new NotImplementedException();

            public async Task CommitAsync(CancellationToken cancellationToken = default)
            {
                await _inner.CommitAsync(cancellationToken);
            }

            public async Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                await _inner.RollbackAsync(cancellationToken);
            }

            public void RegisterDbContext(IDbContextWrapper wrapper)
            {
                _inner.RegisterDbContext(wrapper);
            }

            public void Dispose()
            {
                _inner.Dispose();
                _onDispose?.Invoke();
            }

            public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            {
                return _inner.BeginTransactionAsync(cancellationToken);
            }

            public Task MarkAsCompletedAsync(CancellationToken cancellationToken = default)
            {
                return _inner.MarkAsCompletedAsync(cancellationToken);
            }
        }
    }
}
