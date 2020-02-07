using MiCake.AspNetCore.Modules;
using MiCake.Core.Abstractions.Modularity;

namespace BaseMiCakeApplication
{
    [DependOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {

    }
}
