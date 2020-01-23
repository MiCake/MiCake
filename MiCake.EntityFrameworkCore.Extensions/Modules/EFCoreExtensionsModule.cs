using MiCake.Core.Abstractions.Modularity;
using MiCake.EntityFrameworkCore.Extensions.Audit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.EntityFrameworkCore.Extensions.Modules
{
    public class EFCoreExtensionsModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            //add audit life time
            services.AddTransient<IEfRepositoryLifetime, AuditEFRepositoryLifetime>();
        }
    }
}
