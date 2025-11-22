using System;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.DDD.Infrastructure.Store;
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
    public class MiCakeEFCoreModule : MiCakeModuleAdvanced
    {
        /// <summary>
        /// Indicates this is a framework-level module
        /// </summary>
        public override bool IsFrameworkLevel => true;

        /// <summary>
        /// Configure services for EF Core module
        /// </summary>
        public override void PreConfigureServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            var dbContextType = context.MiCakeApplicationOptions.BuildTimeData.TakeOut<Type>(MiCakeEFCoreModuleInternalKeys.DBContextType)
                                            ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");

            services.TryAddSingleton<IEFSaveChangesLifetime, LazyEFSaveChangesLifetime>();
            services.TryAddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            // Add Uow related services
            services.AddUowCoreServices(dbContextType);
        }

        /// <summary>
        /// Initialize the EF Core module
        /// </summary>
        public override void OnApplicationInitialization(ModuleInitializationContext context)
        {
            // Configure the interceptor factory helper with the factory from DI container
            var factory = context.ServiceProvider.GetService<IMiCakeInterceptorFactory>();
            if (factory != null)
            {
                MiCakeInterceptorFactoryHelper.Configure(factory);
            }

            var efcoreOptions = context.ServiceProvider.GetService<IObjectAccessor<MiCakeEFCoreOptions>>()?.Value
                                        ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");

            var registry = context.ServiceProvider.GetService<IDbContextTypeRegistry>();
            registry?.RegisterDbContextType(efcoreOptions.DbContextType);

            var storeConventionRegistry = context.ApplicationOptions.BuildTimeData.TakeOut<StoreConventionRegistry>(MiCakeEssentialModuleInternalKeys.StoreConventionRegistry);
            if (storeConventionRegistry != null)
                RegisterMiCakeConventions(storeConventionRegistry);
        }

        public override void OnApplicationShutdown(ModuleShutdownContext context)
        {
            MiCakeInterceptorFactoryHelper.Reset();
            base.OnApplicationShutdown(context);
        }

        // Registers the MiCake store conventions into the convention engine(static class)
        private static void RegisterMiCakeConventions(StoreConventionRegistry conventionRegistry)
        {
            var engine = new StoreConventionEngine();
            foreach (var convention in conventionRegistry.Conventions)
            {
                engine.AddConvention(convention);
            }
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }
    }
}
