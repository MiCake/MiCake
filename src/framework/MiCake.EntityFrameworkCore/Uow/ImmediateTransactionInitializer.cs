using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Registry for tracking registered DbContext types in the application.
    /// Consumed by the EF Core module for startup validation.
    /// </summary>
    public interface IDbContextTypeRegistry
    {
        /// <summary>
        /// Register a DbContext type
        /// </summary>
        void RegisterDbContextType(Type dbContextType);

        /// <summary>
        /// Get all registered DbContext types
        /// </summary>
        IReadOnlyList<Type> GetRegisteredTypes();
    }

    /// <summary>
    /// Default implementation of DbContext type registry
    /// </summary>
    public class DbContextTypeRegistry : IDbContextTypeRegistry
    {
        private readonly HashSet<Type> _registeredTypes = [];
        private readonly Lock _lock = new();

        public void RegisterDbContextType(Type dbContextType)
        {
            ArgumentNullException.ThrowIfNull(dbContextType);

            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
            {
                throw new ArgumentException(
                    $"Type {dbContextType.Name} must inherit from DbContext",
                    nameof(dbContextType));
            }

            lock (_lock)
            {
                _registeredTypes.Add(dbContextType);
            }
        }

        public IReadOnlyList<Type> GetRegisteredTypes()
        {
            lock (_lock)
            {
                return [.. _registeredTypes];
            }
        }
    }

    /// <summary>
    /// Implementation of immediate transaction initializer that creates DbContext wrappers
    /// for all registered factories when UoW is configured with immediate initialization.
    /// Uses the typed non-generic factory contract - no reflective best-effort probing.
    /// </summary>
    public class ImmediateTransactionInitializer : IImmediateTransactionInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ImmediateTransactionInitializer> _logger;

        public ImmediateTransactionInitializer(
            IServiceProvider serviceProvider,
            ILogger<ImmediateTransactionInitializer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InitializeTransactionsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);

            // Every DbContext type registered through AddUowCoreServices contributes one
            // non-generic IEFCoreContextFactory registration; resolve them all in one typed pass.
            var factories = _serviceProvider.GetServices<IEFCoreContextFactory>();
            var factoryList = factories as IReadOnlyList<IEFCoreContextFactory> ?? [.. factories];

            if (factoryList.Count == 0)
            {
                _logger.LogWarning(
                    "No DbContext factories registered for immediate transaction initialization. " +
                    "Register DbContext types through UseEFCore<TDbContext>() or AddUowCoreServices().");
                return Task.CompletedTask;
            }

            _logger.LogDebug(
                "Initializing transactions immediately for {Count} registered DbContext factories in UoW {UowId}",
                factoryList.Count,
                unitOfWork.Id);

            foreach (var factory in factoryList)
            {
                // Resolving the wrapper registers the resource with the UoW; transaction activation
                // is performed by the UoW pipeline immediately after the lifecycle hooks complete.
                var wrapper = factory.GetDbContextWrapper();
                _logger.LogDebug(
                    "Initialized resource {ResourceId} ({DbContextType}) in UoW {UowId}",
                    wrapper.Id,
                    wrapper.ResourceType,
                    unitOfWork.Id);
            }

            _logger.LogDebug(
                "Completed immediate transaction initialization for UoW {UowId}",
                unitOfWork.Id);

            return Task.CompletedTask;
        }
    }
}
