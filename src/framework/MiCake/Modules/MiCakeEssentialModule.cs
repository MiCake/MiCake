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
    public static class MiCakeEssentialModuleInternalKeys
    {
        public const string MiCakeAuditSettingOptions = "MiCake.Module.Essential.MiCakeAuditOptions";
        public const string StoreConventionRegistry = "MiCake.Module.Essential.StoreConventionRegistry";
    }

    [RelyOn(typeof(MiCakeRootModule))]
    public class MiCakeEssentialModule : MiCakeModuleAdvanced
    {
        public override bool IsFrameworkLevel => true;

        public override void PreConfigureServices(ModuleConfigServiceContext context)
        {
            var auditOptions = context.MiCakeApplicationOptions.BuildPhaseData.TakeOut(MiCakeEssentialModuleInternalKeys.MiCakeAuditSettingOptions) as MiCakeAuditOptions;
            var services = context.Services;
            var storeConventionRegistry = new StoreConventionRegistry();

            // Register TimeProvider
            services.TryAddSingleton(TimeProvider.System);

            if (auditOptions?.UseAudit == true)
            {
                //Audit Executor
                services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
                //Audit timestamp provider
                services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
                //RepositoryLifeTime
                services.AddScoped<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();

                storeConventionRegistry.AddConvention(new AuditTimeConvention());

                if (auditOptions?.UseSoftDeletion == true)
                {
                    //Audit soft deletion provider
                    services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
                    //RepositoryLifeTime
                    services.AddScoped<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();

                    storeConventionRegistry.AddConvention(new SoftDeletionConvention());
                }
            }

            // Domain Metadata
            services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();

            //LifeTime
            services.TryAddScoped<DomainEventDispatchTracker>();
            services.AddScoped<IRepositoryPreSaveChanges, DomainEventDispatchLifetime>();
            services.AddScoped<IRepositoryPostSaveChanges, DomainEventCleanupLifetime>();

            // Unit of Work - Register with options support
            // Host-local ambient accessor keeps immutable AsyncLocal frames per execution context.
            context.Services.TryAddSingleton<AmbientUnitOfWorkAccessor>();
            // Public singleton accessor lets host-level components (e.g. the EF Core interceptors)
            // locate the provider of the scope owning the ambient unit of work without resolving
            // scoped services from a root-equivalent provider. Registered with AddSingleton so the
            // framework mapping wins over a host-registered replacement; overriding it would
            // desynchronize the ambient state observed by interceptors from the state written by
            // the unit of work manager.
            context.Services.AddSingleton<IUnitOfWorkAmbientAccessor>(
                sp => sp.GetRequiredService<AmbientUnitOfWorkAccessor>());
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.TryAddScoped<IStandaloneUnitOfWorkExecutor, StandaloneUnitOfWorkExecutor>();

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
            context.MiCakeApplicationOptions.BuildPhaseData.Deposit(MiCakeEssentialModuleInternalKeys.StoreConventionRegistry, storeConventionRegistry);
        }
    }
}
