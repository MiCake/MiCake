using MiCake.Core.Abstractions;
using System;

namespace MiCake.Serilog
{
    public static class MiCakeApplcationOptionExtension
    {
        public static IMiCakeApplication AddSerilog(this IMiCakeApplication miCakeApp)
        {
            return AddSerilog(miCakeApp, null);
        }

        public static IMiCakeApplication AddSerilog(this IMiCakeApplication miCakeApp, Action<SerilogConfigureOption> serilogOptionAction)
        {
            serilogOptionAction ??= defaultOption;

            var provide = new SerilogProvider(miCakeApp.Services);
            provide.AddSerilogInMiCake(serilogOptionAction);

            return miCakeApp;

            void defaultOption(SerilogConfigureOption option)
            {
                option.AutoLogError = true;
                option.FilterLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            };
        }
    }
}
