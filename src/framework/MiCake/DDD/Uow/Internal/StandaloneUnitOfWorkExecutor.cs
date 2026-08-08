using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Provider-neutral standalone unit of work executor.
    /// Rejects an existing ambient UoW, creates an isolated DI scope, establishes a root writable UoW,
    /// commits on success, rolls back on failure, and always disposes asynchronously in reverse ownership order.
    /// Contains no persistence-provider dependency.
    /// </summary>
    internal sealed class StandaloneUnitOfWorkExecutor : IStandaloneUnitOfWorkExecutor
    {
        private readonly AmbientUnitOfWorkAccessor _ambientAccessor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StandaloneUnitOfWorkExecutor> _logger;

        public StandaloneUnitOfWorkExecutor(
            AmbientUnitOfWorkAccessor ambientAccessor,
            IServiceScopeFactory scopeFactory,
            ILogger<StandaloneUnitOfWorkExecutor> logger)
        {
            _ambientAccessor = ambientAccessor ?? throw new ArgumentNullException(nameof(ambientAccessor));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task ExecuteAsync(
            Func<IServiceProvider, CancellationToken, Task> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return ExecuteCoreAsync(
                async (provider, ct) =>
                {
                    await operation(provider, ct).ConfigureAwait(false);
                    return true;
                },
                options,
                cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return ExecuteCoreAsync(operation, options, cancellationToken);
        }

        private async Task<TResult> ExecuteCoreAsync<TResult>(
            Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
            UnitOfWorkOptions? options,
            CancellationToken cancellationToken)
        {
            if (_ambientAccessor.Current != null)
            {
                throw new InvalidOperationException(
                    "Standalone unit of work execution requires no ambient unit of work. " +
                    "Use the ambient UoW or ExecuteRequiresNewAsync instead.");
            }

            options ??= UnitOfWorkOptions.Default;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var manager = provider.GetRequiredService<IUnitOfWorkManager>();

            var unitOfWork = await manager.BeginAsync(options, cancellationToken).ConfigureAwait(false);

            return await IsolatedUowExecution.ExecuteAsync(
                    operation,
                    provider,
                    unitOfWork,
                    _logger,
                    "standalone",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
