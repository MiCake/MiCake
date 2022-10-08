using MiCake.Audit.Modules;
using MiCake.Core;

namespace MiCake.Audit
{
    public static class MiCakeBuilderAuditCoreExtension
    {
        /// <summary>
        /// Add MiCake Audit services.
        /// <para>
        ///     For example:Indicates that a class has creation time, modification time, etc.
        ///     You can get some preset time generate sql value form <see cref="PresetAuditConstants"/>
        /// </para>
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="timeGenerateSql">time generate sql. you can get some preset value from <see cref="PresetAuditConstants"/></param>
        public static IMiCakeBuilder UseAudit(this IMiCakeBuilder builder, string timeGenerateSql)
        {
            return UseAudit(builder, s => s.SqlForGenerateTime = timeGenerateSql);
        }

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
            optionsConfig.Invoke(options);

            CheckOptions(options);

            builder.ConfigureApplication((app, services) =>
            {
                //register audit module to micake module collection
                app.SlotModule<MiCakeAuditModule>();

                app.AddStartupTransientData(MiCakeAuditModule.ConfigTransientDataKey, options);
            });

            return builder;
        }

        private static void CheckOptions(MiCakeAuditOptions options)
        {
            if (options.UseSqlToGenerateTime && string.IsNullOrWhiteSpace(options.SqlForGenerateTime))
            {
                throw new ArgumentException($"You are using MiCake audit module and the {nameof(options.UseSqlToGenerateTime)} is true, please config {options.SqlForGenerateTime}. you can get more preset vaule in {nameof(PresetAuditConstants)} class.");
            }
        }
    }
}
