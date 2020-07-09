using AutoMapper;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Modules;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MiCake.EntityFrameworkCore.Tests")]
namespace MiCake.AutoMapper.Modules
{
    [RelyOn(typeof(MiCakeDDDExtensionsModule))]
    public class MiCakeAutoMapperModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void PostConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
        }
    }
}
