using MiCake.AspNetCore.Start;
using MiCake.AutoMapper;
using MiCake.Core.Modularity;

namespace TodoApp
{
    [UseAutoMapper]
    [RelyOn(typeof(MiCakeAutoMapperModule))]
    public class ToDoAppModule : MiCakeStartModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            base.ConfigServices(context);

            // auto register efcore repositories.
            context.AutoRegisterRepositories(typeof(ToDoAppModule).Assembly);
        }
    }
}
