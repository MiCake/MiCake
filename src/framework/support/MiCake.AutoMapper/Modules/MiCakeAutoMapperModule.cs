using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MiCake.AutoMapper
{
    [CoreModule]
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
