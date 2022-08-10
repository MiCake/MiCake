using MiCake.AspNetCore.Modules;
using MiCake.Audit.Modules;
using MiCake.AutoMapper;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;

namespace MiCake.AspNetCore.Start
{
    [RelyOn(typeof(MiCakeAspNetCoreModule), typeof(MiCakeAuditModule), typeof(MiCakeAutoMapperModule))]
    [AutoDI]
    public abstract class MiCakeStartModule : MiCakeModule
    {
    }
}
