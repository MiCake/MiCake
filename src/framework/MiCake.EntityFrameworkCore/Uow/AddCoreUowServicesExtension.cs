using MiCake.DDD.Uow;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Uow
{
    public static class AddCoreUowServicesExtension
    {
        public static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            //DbContext Provider.
            var dbContextProviderServiceType = typeof(IDbContextProvider<>).MakeGenericType(dbContextType);
            var dbContextProvider = typeof(DbContextProvider<>).MakeGenericType(dbContextType);

            services.AddScoped(typeof(IDbContextProvider), dbContextProvider);
            services.AddScoped(dbContextProviderServiceType, dbContextProvider);

            //IDbExecutor
            var dbExecutor = typeof(DbContextExecutor<>).MakeGenericType(dbContextType);
            services.AddTransient(typeof(IDbExecutor), dbExecutor);

            //TransactionProvider
            services.AddTransient<ITransactionProvider, EFCoreTransactionProvider>();

            return services;
        }
    }
}
