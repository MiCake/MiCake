using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using System;

namespace MiCake.Serilog
{
    public static class MiCakeApplicationSerilogExtension
    {
        public static IMiCakeBuilder UseSerilog(this IMiCakeBuilder builder)
        {
            UseSerilog(builder, null);
            return builder;
        }

        public static IMiCakeBuilder UseSerilog(this IMiCakeBuilder builder, Action<SerilogConfigureOption> serilogOptionAction)
        {
            serilogOptionAction ??= defaultOption;

            var provide = new SerilogProvider(builder.Services);
            provide.AddSerilogInMiCake(serilogOptionAction);

            return builder;

            void defaultOption(SerilogConfigureOption option)
            {
                option.AutoLogError = true;
                option.FilterLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            };
        }
    }
}
