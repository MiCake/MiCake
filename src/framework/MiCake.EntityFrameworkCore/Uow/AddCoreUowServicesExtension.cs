using MiCake.DDD.Uow;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Extension methods for registering EF Core Unit of Work services
    /// </summary>
    internal static class AddCoreUowServicesExtension
    {
        /// <summary>
        /// Registers core Unit of Work services for the specified DbContext type.
        /// This includes the context factory, repository dependencies wrapper, and DbContext type registration.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="dbContextType">The DbContext type to register services for</param>
        /// <returns>The service collection for method chaining</returns>
        internal static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            // Register DbContext factory
            var interfaceType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            var implementationType = typeof(EFCoreContextFactory<>).MakeGenericType(dbContextType);
            services.AddScoped(interfaceType, implementationType);

            // Register repository dependencies wrapper for the DbContext
            // This enables the dependency wrapper pattern for repositories
            var dependenciesType = typeof(Repository.EFRepositoryDependencies<>).MakeGenericType(dbContextType);
            services.AddScoped(dependenciesType);

            services.AddScoped<IUnitOfWorkLifetimeHook, ImmediateTransactionLifetimeHook>();
            services.AddSingleton<IDbContextTypeRegistry, DbContextTypeRegistry>();
            services.AddScoped<IImmediateTransactionInitializer, ImmediateTransactionInitializer>();

            return services;
        }
    }
}
