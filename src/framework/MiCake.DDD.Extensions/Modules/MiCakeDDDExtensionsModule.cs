using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Domain.Modules;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Extensions.Lifetime;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Extensions.Modules
{
    [RelyOn(typeof(MiCakeDomainModule))]
    public class MiCakeDDDExtensionsModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddTransient<IDomainObjectModelProvider, DefaultDomainObjectModelProvider>();
            services.AddSingleton<DomainObjectFactory>();
            services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();
            services.AddSingleton(factory =>
            {
                var provider = factory.GetService<IDomainMetadataProvider>();
                return provider.GetDomainMetadata();
            });

            services.AddTransient<IPersistentObjectActivator, PersistentObjectActivator>();

            services.AddScoped(typeof(IRepository<,>), typeof(ProxyRepository<,>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ProxyReadOnlyRepository<,>));
            services.AddScoped(typeof(IRepositoryFactory<,>), typeof(DefaultRepositoryFacotry<,>));
            services.AddScoped(typeof(IFreeRepository<,>), typeof(ProxyFreeRepository<,>));
            services.AddScoped(typeof(IReadOnlyFreeRepository<,>), typeof(ProxyReadOnlyFreeRepository<,>));
            services.AddScoped(typeof(IFreeRepositoryFactory<,>), typeof(DefaultFreeRepositoryFactory<,>));

            //LifeTime
            services.AddScoped<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
        }

        public override void Initialization(ModuleLoadContext context)
        {
            var provider = context.ServiceProvider;

            //activate all mapping relationship between  persistent object and domain object.
            var persistentObjectActivator = provider.GetService<IPersistentObjectActivator>();

            using (var currentScope = provider.CreateScope())
            {
                var mapper = currentScope.ServiceProvider.GetService<IPersistentObjectMapper>();
                if (mapper.InitAtStartup)
                {
                    //need to initialize automatically
                    persistentObjectActivator.SetMapper(mapper);
                    persistentObjectActivator.ActivateMapping();
                }
            }
        }
    }
}
