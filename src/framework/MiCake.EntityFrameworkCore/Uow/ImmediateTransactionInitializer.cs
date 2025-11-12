using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Registry for tracking registered DbContext types in the application.
    /// Used for immediate transaction initialization.
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
    [InjectService(typeof(IDbContextTypeRegistry), Lifetime = MiCakeServiceLifetime.Singleton)]
    public class DbContextTypeRegistry : IDbContextTypeRegistry
    {
        private readonly HashSet<Type> _registeredTypes = new();
        private readonly object _lock = new();

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
                return _registeredTypes.ToList();
            }
        }
    }

    /// <summary>
    /// Implementation of immediate transaction initializer that creates DbContext wrappers
    /// for all registered types when UoW is configured with immediate initialization.
    /// </summary>
    [InjectService(typeof(IImmediateTransactionInitializer), Lifetime = MiCakeServiceLifetime.Scoped)]
    public class ImmediateTransactionInitializer : IImmediateTransactionInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextTypeRegistry _typeRegistry;
        private readonly ILogger<ImmediateTransactionInitializer> _logger;

        public ImmediateTransactionInitializer(
            IServiceProvider serviceProvider,
            IDbContextTypeRegistry typeRegistry,
            ILogger<ImmediateTransactionInitializer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InitializeTransactionsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);

            var registeredTypes = _typeRegistry.GetRegisteredTypes();
            if (registeredTypes.Count == 0)
            {
                _logger.LogWarning(
                    "No DbContext types registered for immediate transaction initialization. " +
                    "Call services.AddMiCakeEFCore<TDbContext>() to register DbContext types.");
                return Task.CompletedTask;
            }

            _logger.LogDebug(
                "Initializing transactions immediately for {Count} registered DbContext types in UoW {UowId}",
                registeredTypes.Count,
                unitOfWork.Id);

            foreach (var dbContextType in registeredTypes)
            {
                try
                {
                    // Get the factory for this DbContext type
                    var factoryType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
                    var factory = _serviceProvider.GetService(factoryType);

                    if (factory == null)
                    {
                        _logger.LogWarning(
                            "No factory registered for DbContext type {DbContextType}. Skipping immediate initialization.",
                            dbContextType.Name);
                        continue;
                    }

                    // Get the DbContext wrapper (this will register it with the UoW and start transaction if configured)
                    var getWrapperMethod = factoryType.GetMethod(nameof(IEFCoreContextFactory<DbContext>.GetDbContextWrapper));
                    if (getWrapperMethod != null)
                    {
                        var wrapper = getWrapperMethod.Invoke(factory, null) as EFCoreDbContextWrapper;
                        
                        if (wrapper != null)
                        {
                            _logger.LogDebug(
                                "Initialized transaction for DbContext type {DbContextType} in UoW {UowId}",
                                dbContextType.Name,
                                unitOfWork.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to initialize transaction for DbContext type {DbContextType} in UoW {UowId}",
                        dbContextType.Name,
                        unitOfWork.Id);
                    throw;
                }
            }

            _logger.LogDebug(
                "Completed immediate transaction initialization for UoW {UowId}",
                unitOfWork.Id);
            
            return Task.CompletedTask;
        }
    }
}
