using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Modules;
using MiCake.DDD.Extensions.Store.Mapping;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MiCake.EntityFrameworkCore.Tests")]
namespace MiCake.Mapster.Modules
{
    /// <summary>
    /// Mapster is a very good object mapping framework, so we use it in micake to achieve object transformation
    /// </summary>
    [RelyOn(typeof(MiCakeDDDExtensionsModule))]
    public class MiCakeMapsterModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            services.TryAddSingleton<IPersistentObjectMapper, MapsterPersistentObjectMapper>();
        }
    }
}
