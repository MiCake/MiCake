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
            IDomainMetadata domainMetadata;
            var domianLayerAsm = context.MiCakeApplicationOptions.DomianLayerAssemblies;

            using (var domainMetadataCreator = new DomainMetadataCreator(context.MiCakeModules, domianLayerAsm))
            {
                domainMetadataCreator.AddDescriptorProvider(new EntityDescriptorProvider());
                domainMetadataCreator.AddDescriptorProvider(new AggregateRootDescriptorProvider(context.MiCakeModules));

                domainMetadata = domainMetadataCreator.Create();
            }

            //auto call storage model ConfigureMapping()
            StorageModelActivator storageModelActivator = new StorageModelActivator(domainMetadata);
            storageModelActivator.LoadConfigMapping();

            context.Services.AddSingleton(domainMetadata);
        }
    }
}
