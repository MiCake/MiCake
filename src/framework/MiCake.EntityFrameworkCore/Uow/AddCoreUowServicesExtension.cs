using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Uow
{
    public static class AddCoreUowServicesExtension
    {
        public static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            //DbContext Provider.
            var interfaceType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            var implementationType = typeof(EFCoreContextFactory<>).MakeGenericType(dbContextType);
            services.AddScoped(interfaceType, implementationType);

            return services;
        }
    }
}
