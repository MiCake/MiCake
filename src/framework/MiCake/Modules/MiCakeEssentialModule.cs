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
using MiCake.DDD.Extensions.Internal;
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

            services.AddScoped(typeof(IRepository<,>), typeof(ProxyRepository<,>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ProxyReadOnlyRepository<,>));
            services.AddScoped(typeof(IRepositoryFactory<,>), typeof(DefaultRepositoryFacotry<,>));

            //LifeTime
            services.AddScoped<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
            services.AddScoped<IRepositoryPostSaveChanges, DomainEventCleanupLifetime>();

            // UOW 
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.TryAddScoped(provider => provider.GetService<IUnitOfWorkManager>().Current);
            context.Services.TryAddTransient<IUnitOfWork, UnitOfWork>();

            //regiter all domain event handler to services
            services.RegisterDomainEventHandler(context.MiCakeModules);

            services.AddScoped<IEventDispatcher, EventDispatcher>();
        }
    }
}
