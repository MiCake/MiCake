using MiCake.Audit.Modules;
using MiCake.Core;
using System;

namespace MiCake.Audit
{
    public static class MiCakeBuilderAuditCoreExtension
    {
        public const string AuditForApplicationOptionsKey = "MiCake.Audit.Key";

        /// <summary>
        /// Add MiCake Audit services.
        /// <para>
        /// For example:Indicates that a class has creation time, modification time, etc
        /// </para>
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="optionsConfig">The config for audit options</param>
        public static IMiCakeBuilder UseAudit(this IMiCakeBuilder builder, Action<MiCakeAuditOptions> optionsConfig)
        {
            var options = new MiCakeAuditOptions();
            optionsConfig?.Invoke(options);

            builder.ConfigureApplication((app, services) =>
            {
                //register audit module to micake module collection
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeAuditModule));

                app.ApplicationOptions.AdditionalInfo.Deposit(AuditForApplicationOptionsKey, options);
            });

            return builder;
        }

        /// <summary>
        /// Add MiCake Audit services.
        /// <para>
        /// For example:Indicates that a class has creation time, modification time, etc
        /// </para>
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        public static IMiCakeBuilder UseAudit(this IMiCakeBuilder builder)
        {
            return UseAudit(builder, null);
        }
    }
}
