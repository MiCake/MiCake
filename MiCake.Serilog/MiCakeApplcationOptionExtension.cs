using MiCake.Core.Abstractions;
using System;

namespace MiCake.Serilog
{
    public static class MiCakeApplcationOptionExtension
    {
        public static IMiCakeApplicationOption UseSerilog(this IMiCakeApplicationOption miCakeOption)
        {
            return UseSerilog(miCakeOption, null);
        }

        public static IMiCakeApplicationOption UseSerilog(this IMiCakeApplicationOption miCakeOption, Action<SerilogConfigureOption> serilogOptionAction)
        {
            serilogOptionAction ??= defaultOption;

            var provide = new SerilogProvider(miCakeOption.Services);
            provide.AddSerilogInMiCake(serilogOptionAction);

            return miCakeOption;

            void defaultOption(SerilogConfigureOption option)
            {
                option.AutoLogError = true;
                option.FilterLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            };
        }
    }
}
