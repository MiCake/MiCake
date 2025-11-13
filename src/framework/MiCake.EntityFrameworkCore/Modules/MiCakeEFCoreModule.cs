using System;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    public class MiCakeEFCoreModuleInternalKeys
    {
        public const string DBContextType = "MiCake.Module.EFCore.DBContextType";
    }

    /// <summary>
    /// MiCake Entity Framework Core module - Provides EF Core integration for MiCake framework
    /// </summary>
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        /// <summary>
        /// Indicates this is a framework-level module
        /// </summary>
        public override bool IsFrameworkLevel => true;

        /// <summary>
        /// Configure services for EF Core module
        /// </summary>
        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            var dbContextType = context.MiCakeApplicationOptions.BuildTimeData.TakeOut<Type>(MiCakeEFCoreModuleInternalKeys.DBContextType)
                                            ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");
           
            services.TryAddSingleton<IEFSaveChangesLifetime, LazyEFSaveChangesLifetime>();
            services.TryAddTransient<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            // Add Uow related services
            services.AddUowCoreServices(dbContextType);
        }

        /// <summary>
        /// Initialize the EF Core module
        /// </summary>
        public override void OnApplicationInitialization(ModuleInitializationContext context)
        {
            // Configure the interceptor factory helper with the factory from DI container
            // This ensures proper dependency injection without using ServiceLocator anti-pattern
            var factory = context.ServiceProvider.GetService<IMiCakeInterceptorFactory>();
            if (factory != null)
            {
                MiCakeInterceptorFactoryHelper.Configure(factory);
            }

            var efcoreOptions = context.ServiceProvider.GetService<IObjectAccessor<MiCakeEFCoreOptions>>().Value
                                        ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");

            var registry = context.ServiceProvider.GetService<IDbContextTypeRegistry>();
            registry?.RegisterDbContextType(efcoreOptions.DbContextType);
        }
    }
}
