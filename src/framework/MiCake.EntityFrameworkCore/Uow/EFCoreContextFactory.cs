using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
        TDbContext GetDbContext();

        /// <summary>
        /// Gets the DbContext wrapper for the current Unit of Work
        /// </summary>
        EFCoreDbContextWrapper GetDbContextWrapper();
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

        /// <summary>
        /// Creates a new EF Core context factory
        /// </summary>
        public EFCoreContextFactory(
            IServiceProvider serviceProvider,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<EFCoreContextFactory<TDbContext>> logger,
            IObjectAccessor<MiCakeEFCoreOptions> efCoreOptions)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _efCoreOptions = efCoreOptions?.Value ?? throw new ArgumentNullException(nameof(efCoreOptions));
        }

        /// <summary>
        /// Gets the DbContext for the current Unit of Work
        /// </summary>
        public TDbContext GetDbContext()
        {
            var wrapper = GetDbContextWrapper();
            return (TDbContext)wrapper.DbContext;
        }

        /// <summary>
        /// Gets the DbContext wrapper for the current Unit of Work.
        /// If <see cref="MiCakeEFCoreOptions.BypassUnitOfWorkCheck"/> is enabled and no UoW is active,
        /// returns a standalone DbContext wrapper without UoW integration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no active Unit of Work is found and <see cref="MiCakeEFCoreOptions.BypassUnitOfWorkCheck"/> is <c>false</c>.
        /// </exception>
        public EFCoreDbContextWrapper GetDbContextWrapper()
        {
            var currentUow = _unitOfWorkManager.Current;

            // Check if UoW is required but not present
            if (currentUow == null)
            {
                if (!_efCoreOptions.BypassUnitOfWorkCheck)
                {
                    throw new InvalidOperationException(
                        $"No active Unit of Work found. Please ensure you're within a Unit of Work scope when accessing {typeof(TDbContext).Name}. " +
                        "You can create one using: using var uow = unitOfWorkManager.Begin(); " +
                        $"If you intentionally want to access DbContext without UoW (e.g., for read-only queries in ResourceFilter/Middleware), " +
                        $"set {nameof(MiCakeEFCoreOptions)}.{nameof(MiCakeEFCoreOptions.BypassUnitOfWorkCheck)} = true.");
                }

                // Bypass mode: return standalone DbContext without UoW integration
                _logger.LogWarning(
                    "Accessing DbContext {DbContextType} without active Unit of Work (BypassUnitOfWorkCheck is enabled). " +
                    "Ensure this is intentional and only used for read-only operations.",
                    typeof(TDbContext).Name);

                return CreateStandaloneWrapper();
            }

            // Normal mode: create wrapper and register with UoW
            return CreateUoWBoundWrapper(currentUow);
        }

        /// <summary>
        /// Creates a standalone DbContext wrapper without UoW integration.
        /// Used when BypassUnitOfWorkCheck is enabled and no UoW is active.
        /// </summary>
        private EFCoreDbContextWrapper CreateStandaloneWrapper()
        {
            TDbContext dbContext;
            try
            {
                dbContext = _serviceProvider.GetRequiredService<TDbContext>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve {typeof(TDbContext).Name} from dependency injection. " +
                    "Ensure the DbContext is properly registered in the DI container.", ex);
            }

            var wrapperLogger = _serviceProvider.GetRequiredService<ILogger<EFCoreDbContextWrapper>>();

            // Don't dispose DbContext since it's managed by DI container
            return new EFCoreDbContextWrapper(dbContext, wrapperLogger, _efCoreOptions, shouldDisposeDbContext: false);
        }

        /// <summary>
        /// Creates a DbContext wrapper bound to the specified Unit of Work.
        /// </summary>
        private EFCoreDbContextWrapper CreateUoWBoundWrapper(IUnitOfWork currentUow)
        {
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

            // Register the wrapper with the current UoW using internal interface
            if (currentUow is IUnitOfWorkInternal internalUow)
            {
                internalUow.RegisterResource(wrapper);

                _logger.LogDebug("Registered EFCore DbContext wrapper for {DbContextType} with identifier {ResourceIdentifier} in UoW {UowId}",
                    typeof(TDbContext).Name, wrapper.ResourceIdentifier, currentUow.Id);
            }
            else
            {
                _logger.LogWarning("Current UoW does not implement IUnitOfWorkInternal, DbContext wrapper not registered");
            }

            return wrapper;
        }
    }
}
