using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Aop.Castle.Modules
{
    public class MiCakeAopCastleModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddSingleton<IMiCakeProxyProvider, CastleMiCakeProxyProvider>();
        }

        public override void Initialization(ModuleBearingContext context)
        {
            base.Initialization(context);
        }
    }
}
