using MiCake.Core.Abstractions.ExceptionHandling;
using MiCake.Core.Abstractions.Logging;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Serilog.ExceptionHanding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Serilog
{
    public class MiCakeSerilogModule : MiCakeModule
    {
        public MiCakeSerilogModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void Initialization(ModuleBearingContext context)
        {
        }

        public override void PostConfigServices(ModuleConfigServiceContext context)
        {
            var micakeErrorHandler = GetServiceFromCollection<IMiCakeErrorHandler>(context.Services);
            var serilogHandlerProvide = GetServiceFromCollection<ILogErrorHandlerProvider>(context.Services);

            micakeErrorHandler?.ConfigureHandlerService(serilogHandlerProvide.GetErrorHandler());
        }

        public override void PostModuleInitialization(ModuleBearingContext context)
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void PreModuleInitialization(ModuleBearingContext context)
        {
        }

        public override void PreModuleShutDown(ModuleBearingContext context)
        {
        }

        public override void Shuntdown(ModuleBearingContext context)
        {
        }
    }
}
