using System;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.DDD.Infrastructure.Store;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    public static class MiCakeEFCoreModuleInternalKeys
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
            var dbContextType = context.MiCakeApplicationOptions.BuildPhaseData.TakeOut<Type>(MiCakeEFCoreModuleInternalKeys.DBContextType)
                                            ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");

            services.TryAddScoped<IEFSaveChangesLifetime, LazyEFSaveChangesLifetime>();
            services.TryAddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            // Add Uow related services
            services.AddUowCoreServices(dbContextType);
        }

        /// <summary>
        /// Initialize the EF Core module
        /// </summary>
        public override void OnApplicationInitialization(ModuleInitializationContext context)
        {
            var efcoreOptions = context.ServiceProvider.GetService<IObjectAccessor<MiCakeEFCoreOptions>>()?.Value
                                        ?? throw new InvalidOperationException("Invaild Operation. Please make sure you have configured MiCake EFCore module through UseEFCore() method when building MiCake application.");

            var registry = context.ServiceProvider.GetService<IDbContextTypeRegistry>();
            if (registry != null)
            {
                registry.RegisterDbContextType(efcoreOptions.DbContextType);
                ValidateDbContextLifetimes(context.ServiceProvider, registry);
                ValidateExecutionStrategies(context.ServiceProvider, registry);
            }

            var storeConventionRegistry = context.ApplicationOptions.BuildPhaseData.TakeOut<StoreConventionRegistry>(MiCakeEssentialModuleInternalKeys.StoreConventionRegistry);
            if (storeConventionRegistry != null)
                RegisterMiCakeConventions(storeConventionRegistry);
        }

        /// <summary>
        /// Validates the effective DbContext lifetime from the built service provider.
        /// A supported scoped or pooled registration returns one instance within a scope and
        /// distinct instances from two simultaneously active scopes.
        /// </summary>
        internal static void ValidateDbContextLifetimes(IServiceProvider serviceProvider, IDbContextTypeRegistry registry)
        {
            foreach (var dbContextType in registry.GetRegisteredTypes())
            {
                using var firstScope = serviceProvider.CreateScope();
                using var secondScope = serviceProvider.CreateScope();

                var first = firstScope.ServiceProvider.GetRequiredService(dbContextType);
                var firstAgain = firstScope.ServiceProvider.GetRequiredService(dbContextType);
                var second = secondScope.ServiceProvider.GetRequiredService(dbContextType);

                if (!ReferenceEquals(first, firstAgain) || ReferenceEquals(first, second))
                {
                    throw new InvalidOperationException(
                        $"DbContext {dbContextType.Name} does not have a supported effective lifetime. " +
                        "MiCake requires scoped or pooled DbContext registrations so each scope owns one stable context instance.");
                }
            }
        }

        /// <summary>
        /// Validates that no registered DbContext uses an execution strategy that retries on failure.
        /// Retrying strategies inside an ambient writable UoW can re-execute a partially completed
        /// command sequence; MiCake requires application-owned replayable boundaries instead.
        /// </summary>
        internal static void ValidateExecutionStrategies(IServiceProvider serviceProvider, IDbContextTypeRegistry registry)
        {
            foreach (var dbContextType in registry.GetRegisteredTypes())
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);

                var executionStrategy = dbContext.Database.GetService<IExecutionStrategyFactory>().Create();
                if (executionStrategy.RetriesOnFailure)
                {
                    throw new InvalidOperationException(
                        $"DbContext {dbContextType.Name} is configured with an execution strategy that retries on failure " +
                        "(RetriesOnFailure=true). MiCake ambient writable units of work require application-owned replayable " +
                        "boundaries; remove retry-on-failure configuration or wrap the operation in a replayable boundary.");
                }
            }
        }

        public override void OnApplicationShutdown(ModuleShutdownContext context)
        {
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
