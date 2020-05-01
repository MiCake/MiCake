using MiCake.Audit.Modules;
using MiCake.Core;

namespace MiCake.Audit
{
    public static class MiCakeBuilderAuditCoreExtension
    {
        /// <summary>
        /// Add MiCake Audit services.
        /// <para>
        /// For example:Indicates that a class has creation time, modification time, etc
        /// </para>
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
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
