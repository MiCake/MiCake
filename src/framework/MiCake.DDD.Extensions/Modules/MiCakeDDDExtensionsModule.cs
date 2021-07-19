using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Modules;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Extensions.Lifetime;
using MiCake.DDD.Extensions.Metadata;
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

            services.AddScoped(typeof(IRepository<,>), typeof(ProxyRepository<,>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ProxyReadOnlyRepository<,>));
            services.AddScoped(typeof(IRepositoryFactory<,>), typeof(DefaultRepositoryFacotry<,>));

            //LifeTime
            services.AddTransient<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
        }

        public override void Initialization(ModuleLoadContext context)
        {
        }
    }
}
