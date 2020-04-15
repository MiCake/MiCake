using MiCake.Audit.Modules;
using MiCake.Core;

namespace MiCake.Audit
{
    public static class MiCakeBuilderAuditCoreExtension
    {
        public static IMiCakeBuilder UseAudit(this IMiCakeBuilder builder)
        {
            builder.ConfigureApplication((app, services) =>
            {
                //register audit module to micake module collection
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeAuditModule));
            });

            return builder;
        }
    }
}
