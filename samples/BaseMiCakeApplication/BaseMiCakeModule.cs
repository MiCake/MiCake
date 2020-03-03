using MiCake.AspNetCore.Modules;
using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    [DependOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {

    }
}
