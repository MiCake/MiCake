using MiCake.Core.Modularity;
using MiCake.DDD.Domain.Modules;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Extensions.Store;
using MiCake.Mapster.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Extensions.Modules
{
    [DependOn(typeof(MiCakeMapsterModule), typeof(MiCakeDomainModule))]
    public class MiCakeDDDExtensionsModule : MiCakeModule
    {
        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddTransient<IDomainObjectModelProvider, DefaultDomainObjectModelProvider>();
            services.AddSingleton<DomainObjectFactory>();
            services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();
            services.AddSingleton<DomainMetadata>(factory =>
            {
                var provider = factory.GetService<IDomainMetadataProvider>();
                return provider.GetDomainMetadata();
            });

            services.AddSingleton<IStorageModelActivator, StorageModelActivator>();
        }
    }
}
