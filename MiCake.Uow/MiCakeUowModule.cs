using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Uow
{
    public class MiCakeUowModule : MiCakeModule
    {
        public MiCakeUowModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddSingleton<IUnitOfWorkManager, UnitOfWorkManager>();
        }

        public override void Initialization(ModuleBearingContext context)
        {
        }
    }
}
