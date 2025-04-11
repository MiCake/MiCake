﻿using System.Threading.Tasks;
using MiCake.Audit;
using MiCake.Audit.Core;
using MiCake.Audit.Lifetime;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Store;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Extensions.Lifetime;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Extensions.Store.Configure;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Modules
{
    public class MiCakeEssentialModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override Task PreConfigServices(ModuleConfigServiceContext context)
        {
            StoreConfig.Instance.AddModelProvider(new SoftDeletionStoreEntityConfig());

            return Task.CompletedTask;
        }

        public override Task ConfigServices(ModuleConfigServiceContext context)
        {
            var auditOptions = (MiCakeAuditOptions)context.MiCakeApplicationOptions.ExtraDataStash.TakeOut(MiCakeBuilderAuditCoreExtension.AuditForApplicationOptionsKey);
            var services = context.Services;

            if (auditOptions?.UseAudit == true)
            {
                //Audit Executor
                services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
                //Audit CreationTime and ModifationTime
                services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
                //RepositoryLifeTime
                services.AddTransient<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();

                if (auditOptions?.UseSoftDeletion == true)
                {
                    //Audit Deletion Time
                    services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
                    //RepositoryLifeTime
                    services.AddTransient<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();
                }
            }

            services.AddTransient<IDomainObjectModelProvider, DefaultDomainObjectModelProvider>();
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
            services.AddTransient<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();

            // UOW 
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.TryAddScoped<ICurrentUnitOfWork, CurrentUnitOfWork>();
            context.Services.TryAddTransient<IUnitOfWork, UnitOfWork>();

            //regiter all domain event handler to services
            services.ResigterDomainEventHandler(context.MiCakeModules);

            services.AddScoped<IEventDispatcher, EventDispatcher>();

            return Task.CompletedTask;
        }
    }
}
