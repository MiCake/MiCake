using MiCake.Core;
using System;

namespace MiCake.Serilog
{
    public static class MiCakeApplicationSerilogExtension
    {
        public static IMiCakeBuilder AddSerilog(this IMiCakeBuilder builder)
        {
            AddSerilog(builder, null);
            return builder;
        }

        public static IMiCakeBuilder AddSerilog(this IMiCakeBuilder builder, Action<SerilogConfigureOption> serilogOptionAction)
        {
            //regist serilog to micake framework
            builder.ConfigureApplication(s => s.ModuleManager.AddFeatureModule(typeof(MiCakeSerilogModule)));
            // var provide = new SerilogProvider(builder.Services);
            // provide.AddSerilogInMiCake(serilogOptionAction ??= defaultOption);
            return builder;

            //void defaultOption(SerilogConfigureOption option)
            //{
            //    option.AutoLogError = true;
            //    option.FilterLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            //};
        }
    }
}
