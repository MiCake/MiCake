using MiCake.Core;
using MiCake.SqlReader;
using System;

namespace MiCake
{
    public static class MiCakeSqlReaderExtension
    {
        /// <summary>
        /// Add SqlReader for MiCake
        /// </summary>
        public static IMiCakeBuilder UseSqlReader(this IMiCakeBuilder builder, Action<MiCakeSqlReaderOptions> optionsAction = null)
        {
            builder.ConfigureApplication((app, services) =>
            {
                services.AddSqlReader(optionsAction);
            });

            return builder;
        }
    }
}
