using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
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
            //regist serilog to micake framework
            builder.ModuleManager.AddFeatureModule(new MiCakeSerilogModule() { AutoRegisted = true, Order = FeatureModuleLoadOrder.BeforeCommonModule });
            
            var provide = new SerilogProvider(builder.Services);
            provide.AddSerilogInMiCake(serilogOptionAction ??= defaultOption);

            return builder;

            void defaultOption(SerilogConfigureOption option)
            {
                option.AutoLogError = true;
                option.FilterLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            };
        }
    }
}
