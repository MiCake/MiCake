using AutoMapper;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Modules;
using MiCake.DDD.Extensions.Store.Mapping;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MiCake.EntityFrameworkCore.Tests")]
namespace MiCake.AutoMapper.Modules
{
    [RelyOn(typeof(MiCakeDDDExtensionsModule))]
    public class MiCakeAutoMapperModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddScoped<IPersistentObjectMapper, AutoMapperPersistentObjectMapper>();
        }

        public override void PostConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddAutoMapper(cfg =>
            {
                cfg.DisableConstructorMapping();
                cfg.ConfigPersistentObject(services);
            },
            typeof(MiCakeAutoMapperModule));
        }
    }
}
