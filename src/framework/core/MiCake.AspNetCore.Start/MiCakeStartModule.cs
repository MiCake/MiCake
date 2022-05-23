using MiCake.AspNetCore.Modules;
using MiCake.Audit.Modules;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;

namespace MiCake.AspNetCore.Start
{
    [RelyOn(typeof(MiCakeAspNetCoreModule), typeof(MiCakeAuditModule))]
    [AutoDI]
    public abstract class MiCakeStartModule : MiCakeModule
    {
    }
}
