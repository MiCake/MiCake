using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Dapper.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Dapper
{
    public static class MiCakeDapperExtension
    {
        /// <summary>
        /// Add micake dapper support.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString">your db connection string.</param>
        /// <returns></returns>
        public static IMiCakeBuilder UseDapper(this IMiCakeBuilder builder, string connectionString)
        {
            CheckValue.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            return UseDapper(builder, s => { s.DbConnectionStr = connectionString; });
        }

        /// <summary>
        /// Add micake dapper support.
        /// </summary>
        public static IMiCakeBuilder UseDapper(this IMiCakeBuilder builder, Action<MiCakeDapperOptions>? optionsAction = null)
        {
            MiCakeDapperOptions options = new();
            optionsAction?.Invoke(options);

            CheckValue.NotNullOrWhiteSpace(options.DbConnectionStr, nameof(options.DbConnectionStr));

            builder.ConfigureApplication((app, services) =>
            {
                app.SlotModule<MiCakeDapperModule>();

                services.Configure<MiCakeDapperOptions>(s => s.Apply(options));
            });

            return builder;
        }
    }
}
