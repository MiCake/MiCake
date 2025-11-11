using System;
using System.Threading.Tasks;
using MiCake.Audit;
using MiCake.Audit.Core;
using MiCake.Audit.Lifetime;
using MiCake.Audit.SoftDeletion;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Lifetime;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Modules
{
    public class MiCakeEssentialModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            var auditOptions = (MiCakeAuditOptions)context.MiCakeApplicationOptions.BuildTimeData.TakeOut(MiCakeBuilderAuditCoreExtension.AuditForApplicationOptionsKey);
            var services = context.Services;

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

                if (auditOptions?.UseSoftDeletion == true)
                {
                    //Audit Deletion Time
                    services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
                    //RepositoryLifeTime
                    services.AddScoped<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();
                }
            }

            services.AddSingleton<IDomainObjectModelProvider, DefaultDomainObjectModelProvider>();
            services.AddSingleton<DomainObjectFactory>();
            services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();
            services.AddSingleton(factory =>
            {
                var provider = factory.GetService<IDomainMetadataProvider>();
                return provider.GetDomainMetadata();
            });

            // Note: IRepository<,> and IReadOnlyRepository<,> are NO LONGER auto-registered
            // Users should either:
            // 1. Create custom repositories inheriting from persistence layer base (e.g., EFRepositoryBase)
            // 2. Inject IRepositoryProvider<,> directly for simple cases

            //LifeTime
            services.AddScoped<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
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

            //regiter all domain event handler to services
            services.RegisterDomainEventHandler(context.MiCakeModules);

            services.AddScoped<IEventDispatcher, EventDispatcher>();
        }
    }
}
