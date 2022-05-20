using MiCake.AspNetCore.Modules;
using MiCake.Audit.Modules;
using MiCake.AutoMapper;
using MiCake.Core.Modularity;

namespace TodoApp
{
    [UseAutoMapper]
    [RelyOn(typeof(MiCakeAspNetCoreModule), typeof(MiCakeAuditModule))]
    public class ToDoAppModule : MiCakeModule
    {
    }
}
