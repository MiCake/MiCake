using MiCake.Core;
using MiCake.SqlReader.Modules;

namespace MiCake.SqlReader
{
    public static class MiCakeSqlReaderExtension
    {
        /// <summary>
        /// Add SqlReader for MiCake
        /// </summary>
        public static IMiCakeBuilder UseSqlReader(this IMiCakeBuilder builder, Action<MiCakeSqlReaderOptions>? optionsAction = null)
        {
            builder.ConfigureApplication((app, services) =>
            {
                app.SlotModule<MiCakeSqlReaderModule>();

                services.AddSqlReader(optionsAction);
            });

            return builder;
        }
    }
}
