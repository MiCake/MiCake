using MiCake.Core;

namespace MiCake.Audit.Modules
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
        /// <param name="optionsConfig">The config for audit options</param>
        public static IMiCakeBuilder UseAudit(this IMiCakeBuilder builder, Action<MiCakeAuditOptions>? optionsConfig = null)
        {
            var options = new MiCakeAuditOptions();
            optionsConfig?.Invoke(options);

            builder.ConfigureApplication((app, services) =>
            {
                //register audit module to micake module collection
                app.SlotModule<MiCakeAuditModule>();

                app.AddStartupTransientData(MiCakeAuditModule.ConfigTransientDataKey, options);
            });

            return builder;
        }
    }
}
