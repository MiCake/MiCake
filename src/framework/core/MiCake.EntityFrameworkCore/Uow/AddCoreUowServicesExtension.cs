using MiCake.Uow;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Uow
{
    public static class AddCoreUowServicesExtension
    {
        public static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
        {
            //TransactionProvider
            services.AddTransient<ITransactionProvider, EFCoreTransactionProvider>();

            return services;
        }
    }
}
