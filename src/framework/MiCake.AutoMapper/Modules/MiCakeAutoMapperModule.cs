using AutoMapper;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Modules;
using System.Linq;
using System.Reflection;

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
            var modules = context.MiCakeModules;

            //Select UseAutoMapperAttribute module.
            var autoMapperAsms = modules.Where(s => s.ModuleType.GetCustomAttribute<UseAutoMapperAttribute>() != null)
                                        .Select(s => s.Assembly).ToArray();

            services.AddAutoMapper(cfg =>
            {
                cfg.DisableConstructorMapping();
            },
            autoMapperAsms);
        }
    }
}
