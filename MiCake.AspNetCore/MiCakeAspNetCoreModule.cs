using MiCake.Autofac;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.AspNetCore
{
    [DependOn(
        typeof(MiCakeSerilogModule),
        typeof(MiCakeAutofacModule))]
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public MiCakeAspNetCoreModule()
        {
        }
    }
}
