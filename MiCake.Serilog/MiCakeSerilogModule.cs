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

        public override void OnShuntdown(ModuleContext context)
        {
        }

        public override void OnStart(ModuleContext context)
        {
            var serviceProvide = context.Services.BuildServiceProvider();
            var micakeErrorHandler =  serviceProvide.GetService<IMiCakeErrorHandler>();
            var serilogHandlerProvide = serviceProvide.GetService<ILogErrorHandlerProvider>();

            micakeErrorHandler?.ConfigureHandlerService(serilogHandlerProvide.GetErrorHandler());
        }

        public override void PreShuntdown(ModuleContext context)
        {
        }

        public override void PreStart(ModuleContext context)
        {
        }

        public override void Shuntdown(ModuleContext context)
        {
        }

        public override void Start(ModuleContext context)
        {
        }
    }
}
