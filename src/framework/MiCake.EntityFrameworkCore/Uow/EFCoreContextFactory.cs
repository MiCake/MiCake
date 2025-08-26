using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Factory for creating EF Core DbContext wrappers that integrates with Unit of Work
    /// </summary>
    public interface IEFCoreContextFactory<TDbContext> where TDbContext : DbContext
    {
        /// <summary>
        /// Gets the DbContext for the current Unit of Work
        /// </summary>
        Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the DbContext wrapper for the current Unit of Work
        /// </summary>
        Task<EFCoreDbContextWrapper> GetDbContextWrapperAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of EF Core DbContext factory that properly handles scope and lifetime issues
    /// </summary>
    public class EFCoreContextFactory<TDbContext> : IEFCoreContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<EFCoreContextFactory<TDbContext>> _logger;
        private readonly MiCakeEFCoreOptions _efCoreOptions;

        public EFCoreContextFactory(
            IServiceProvider serviceProvider,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<EFCoreContextFactory<TDbContext>> logger,
            IObjectAccessor<MiCakeEFCoreOptions> efCoreOptions)
        {
            _serviceProvider = serviceProvider;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
            _efCoreOptions = efCoreOptions.Value;
        }

        public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            var isUsingImplicitMode = _efCoreOptions.ImplicitModeForUow;
            if (isUsingImplicitMode && _unitOfWorkManager.Current == null)
            {
                return _serviceProvider.GetRequiredService<TDbContext>();
            }

            var wrapper = await GetDbContextWrapperAsync(cancellationToken);
            return (TDbContext)wrapper.DbContext;
        }

        public Task<EFCoreDbContextWrapper> GetDbContextWrapperAsync(CancellationToken cancellationToken = default)
        {
            var currentUow = _unitOfWorkManager.Current ?? throw new InvalidOperationException(
                       $"No active Unit of Work found. Please ensure you're within a Unit of Work scope when accessing {typeof(TDbContext).Name}. " +
                       "You can create one using: using var uow = unitOfWorkManager.Begin();");

            TDbContext dbContext;
            try
            {
                // Get DbContext from DI - this respects the configured lifetime
                dbContext = _serviceProvider.GetRequiredService<TDbContext>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve {typeof(TDbContext).Name} from dependency injection. " +
                    "Ensure the DbContext is properly registered in the DI container.", ex);
            }

            var wrapperLogger = _serviceProvider.GetRequiredService<ILogger<EFCoreDbContextWrapper>>();

            // Don't dispose DbContext since it's managed by DI container (could be singleton, scoped, etc.)
            var wrapper = new EFCoreDbContextWrapper(dbContext, wrapperLogger, _efCoreOptions, shouldDisposeDbContext: false);

            // Register the wrapper with the current uow (will be skipped if already registered for same DbContext instance)
            currentUow.RegisterDbContext(wrapper);

            _logger.LogDebug("Retrieved EFCore DbContext wrapper for {DbContextType} with identifier {ContextIdentifier} in UoW {UowId}",
                typeof(TDbContext).Name, wrapper.ContextIdentifier, currentUow.Id);

            return Task.FromResult(wrapper);
        }
    }
}
