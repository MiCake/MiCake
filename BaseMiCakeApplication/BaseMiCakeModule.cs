using MiCake.AspNetCore;
using MiCake.Core.Abstractions.Modularity;

namespace BaseMiCakeApplication
{
    [DependOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {

    }
}
