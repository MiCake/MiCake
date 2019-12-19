using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow.Easy
{
    public class MiCakeUowEasyModule : MiCakeModule
    {
        public MiCakeUowEasyModule()
        {
        }


        //添加需要所需要的注入服务
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddSingleton<IUnitOfWorkManager, UnitOfWorkManager>();
        }
    }
}
