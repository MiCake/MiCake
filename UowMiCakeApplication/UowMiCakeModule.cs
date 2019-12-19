using MiCake.AspNetCore;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Uow.Easy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UowMiCakeApplication
{
    [DependOn(typeof(MiCakeAspNetCoreModule),typeof(MiCakeUowEasyModule))]
    public class UowMiCakeModule:MiCakeModule
    {
    }
}
