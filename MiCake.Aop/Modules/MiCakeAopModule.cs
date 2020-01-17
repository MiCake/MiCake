using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Aop.Modules
{
    public class MiCakeAopModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddTransient(provider =>
            {
                return provider.GetService<IMiCakeProxyProvider>().GetMiCakeProxy();
            });
        }
    }
}
