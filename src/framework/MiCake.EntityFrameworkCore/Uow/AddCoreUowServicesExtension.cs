using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Extension methods for registering EF Core Unit of Work services
    /// </summary>
    internal static class AddCoreUowServicesExtension
    {
        /// <summary>
        /// Registers core Unit of Work services for the specified DbContext type.
        /// This includes the context factory (generic and non-generic), repository dependencies wrapper,
        /// the immediate transaction hook/initializer, and the DbContext type registry.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="dbContextType">The DbContext type to register services for</param>
        /// <returns>The service collection for method chaining</returns>
        internal static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(dbContextType);

            // DbContext lifetime validation: scoped and pooled (which registers as scoped) pass;
            // singleton/transient registrations produce inconsistent tracking and are rejected.
            ValidateDbContextRegistration(services, dbContextType);

            // Register DbContext factory
            var interfaceType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            var implementationType = typeof(EFCoreContextFactory<>).MakeGenericType(dbContextType);
            services.AddScoped(interfaceType, implementationType);

            // Non-generic registration enables typed immediate initialization without reflection.
            services.AddScoped(typeof(IEFCoreContextFactory), sp => (IEFCoreContextFactory)sp.GetRequiredService(interfaceType));

            // Register repository dependencies wrapper for the DbContext
            // This enables the dependency wrapper pattern for repositories
            var dependenciesType = typeof(Repository.EFRepositoryDependencies<>).MakeGenericType(dbContextType);
            services.AddScoped(dependenciesType);

            // Explicit physical-operation executor for lifecycle-bypassing deletes
            var physicalExecutorInterface = typeof(Repository.IEFCorePhysicalOperationExecutor<>).MakeGenericType(dbContextType);
            var physicalExecutorImplementation = typeof(Repository.EFCorePhysicalOperationExecutor<>).MakeGenericType(dbContextType);
            services.AddScoped(physicalExecutorInterface, physicalExecutorImplementation);

            services.TryAddScoped<IUnitOfWorkLifetimeHook, ImmediateTransactionLifetimeHook>();
            services.TryAddSingleton<IDbContextTypeRegistry, DbContextTypeRegistry>();
            services.TryAddScoped<IImmediateTransactionInitializer, ImmediateTransactionInitializer>();

            // Write guard pipeline: scoped coordinator resolved by the interceptors from the
            // saving/command context's provider.
            services.TryAddScoped<IEFCoreWriteCoordinator, EFCoreWriteCoordinator>();

            return services;
        }

        /// <summary>
        /// Validates that the DbContext is registered with a supported lifetime.
        /// Scoped and pooled (DI-scoped) registrations pass; missing or singleton/transient
        /// registrations fail with a context-specific diagnostic. The effective registration is
        /// the last descriptor for the type, matching the container's single-service resolution.
        /// </summary>
        internal static void ValidateDbContextRegistration(IServiceCollection services, Type dbContextType)
        {
            var descriptor = services.LastOrDefault(d => d.ServiceType == dbContextType);
            if (descriptor == null)
            {
                throw new InvalidOperationException(
                    $"DbContext {dbContextType.Name} is not registered in the service collection. " +
                    "Register it with AddDbContext<TDbContext>() or AddDbContextPool<TDbContext>() before enabling the MiCake EF Core module.");
            }

            if (descriptor.Lifetime != ServiceLifetime.Scoped)
            {
                throw new InvalidOperationException(
                    $"DbContext {dbContextType.Name} is registered with lifetime {descriptor.Lifetime}, which is not supported. " +
                    "MiCake requires scoped or pooled DbContext registrations (AddDbContext / AddDbContextPool) " +
                    "to guarantee one stable context per unit of work.");
            }
        }
    }
}
