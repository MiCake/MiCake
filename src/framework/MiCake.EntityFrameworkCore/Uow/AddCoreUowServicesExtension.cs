using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Extension methods for registering EF Core Unit of Work services
    /// </summary>
    public static class AddCoreUowServicesExtension
    {
        /// <summary>
        /// Registers core Unit of Work services for the specified DbContext type.
        /// This includes the context factory, repository dependencies wrapper, and DbContext type registration.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="dbContextType">The DbContext type to register services for</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            // Register DbContext factory
            var interfaceType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            var implementationType = typeof(EFCoreContextFactory<>).MakeGenericType(dbContextType);
            services.AddScoped(interfaceType, implementationType);

            // Register repository dependencies wrapper for the DbContext
            // This enables the dependency wrapper pattern for repositories
            var dependenciesType = typeof(Repository.EFRepositoryDependencies<>).MakeGenericType(dbContextType);
            services.AddScoped(dependenciesType);

            // Register the DbContext type in the registry for immediate transaction initialization support
            // This is done by ensuring the registry is available and registering the type
            var sp = services.BuildServiceProvider();
            var registry = sp.GetService<IDbContextTypeRegistry>();
            if (registry != null)
            {
                registry.RegisterDbContextType(dbContextType);
            }

            return services;
        }
    }
}
