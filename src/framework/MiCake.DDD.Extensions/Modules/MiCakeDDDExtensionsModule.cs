using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Metadata;
using MiCake.Mapster.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Extensions.Modules
{
    [DependOn(typeof(MiCakeMapsterModule))]
    public class MiCakeDDDExtensionsModule : MiCakeModule
    {
        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            IDomainMetadata domainMetadata;
            using (var domainMetadataCreator = new DomainMetadataCreator(context.MiCakeModules))
            {
                domainMetadata = domainMetadataCreator.Create();
            }

            context.Services.AddSingleton<IDomainMetadata>(domainMetadata);
        }
    }
}
