using System;
using MiCake.Audit;
using MiCake.Audit.Conventions;
using MiCake.Audit.Core;
using MiCake.Audit.Lifetime;
using MiCake.Audit.SoftDeletion;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Infrastructure.Metadata;
using MiCake.DDD.Infrastructure.Store;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Modules
{
    public class MiCakeEssentialModuleInternalKeys
    {
        public const string MiCakeAuditSettingOptions = "MiCake.Module.Essential.MiCakeAuditOptions";
        public const string StoreConventionRegistry = "MiCake.Module.Essential.StoreConventionRegistry";
    }

    [RelyOn(typeof(MiCakeRootModule))]
    public class MiCakeEssentialModule : MiCakeModuleAdvanced
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            var auditOptions = (MiCakeAuditOptions)context.MiCakeApplicationOptions.BuildTimeData.TakeOut(MiCakeEssentialModuleInternalKeys.MiCakeAuditSettingOptions);
            var services = context.Services;
            var storeConventionRegistry = new StoreConventionRegistry();

            if (auditOptions?.UseAudit == true)
            {
                //Audit Executor
                services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
                //Audit CreationTime and ModifationTime
                services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
                //RepositoryLifeTime
                services.AddScoped<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();

                if (auditOptions?.AuditTimeProvider is not null)
                {
                    DefaultTimeAuditProvider.CurrentTimeProvider = auditOptions.AuditTimeProvider;
                }
                storeConventionRegistry.AddConvention(new AuditTimeConvention());

                if (auditOptions?.UseSoftDeletion == true)
                {
                    //Audit Deletion Time
                    services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
                    //RepositoryLifeTime
                    services.AddScoped<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();

                    storeConventionRegistry.AddConvention(new SoftDeletionConvention());
                }
            }

            // Domain Metadata
            services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();

            //LifeTime
            services.AddScoped<IRepositoryPreSaveChanges, DomainEventDispatchLifetime>();
            services.AddScoped<IRepositoryPostSaveChanges, DomainEventCleanupLifetime>();

            // Unit of Work - Register with options support
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();

            // Register current UoW accessor (returns Current from manager, may be null)
            services.TryAddScoped(provider =>
            {
                var manager = provider.GetRequiredService<IUnitOfWorkManager>();
                return manager.Current ?? throw new InvalidOperationException(
                    "No active Unit of Work. Call IUnitOfWorkManager.Begin() to start a new Unit of Work.");
            });

            //register all domain event handler to services
            services.RegisterDomainEventHandler(context.MiCakeModules);
            services.AddScoped<IEventDispatcher, EventDispatcher>();

            // Store Convention Registry to build chain
            context.MiCakeApplicationOptions.BuildTimeData.Deposit(MiCakeEssentialModuleInternalKeys.StoreConventionRegistry, storeConventionRegistry);
        }

        public override void PostConfigureServices(ModuleConfigServiceContext context)
        {
             var storeConventionRegistry = context.MiCakeApplicationOptions.BuildTimeData.TakeOut<StoreConventionRegistry>(MiCakeEssentialModuleInternalKeys.StoreConventionRegistry);
            CreateConventionEngine(storeConventionRegistry);
        }

        private static StoreConventionEngine CreateConventionEngine(StoreConventionRegistry conventionRegistry)
        {
            var engine = new StoreConventionEngine();

            // Register all configured conventions
            foreach (var convention in conventionRegistry.Conventions)
            {
                engine.AddConvention(convention);
            }

            return engine;
        }
    }
}
