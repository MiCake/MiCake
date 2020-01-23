using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Identity.Modules
{
    public class MiCakeIdentityModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
        }
    }
}
