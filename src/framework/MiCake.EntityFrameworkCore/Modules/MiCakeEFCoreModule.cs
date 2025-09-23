using System.Threading.Tasks;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override Task PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            // Register the lazy singleton version of IEFSaveChangesLifetime
            // This allows the interceptor to work even when created at application startup
            services.TryAddSingleton<IEFSaveChangesLifetime, LazyEFSaveChangesLifetime>();
            
            //add ef repository provider
            services.AddScoped(typeof(IRepositoryProvider<,>), typeof(EFRepositoryProvider<,>));
            
            // Register the interceptor factory
            services.TryAddTransient<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            return Task.CompletedTask;
        }

        public override Task PostInitialization(ModuleLoadContext context)
        {
            // Configure the interceptor factory helper with the factory from DI container
            // This ensures proper dependency injection without using ServiceLocator anti-pattern
            var factory = context.ServiceProvider.GetService<IMiCakeInterceptorFactory>();
            if (factory != null)
            {
                MiCakeInterceptorFactoryHelper.Configure(factory);
            }

            return base.PostInitialization(context);
        }
    }
}
