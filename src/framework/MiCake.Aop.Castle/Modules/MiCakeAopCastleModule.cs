using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Aop.Castle.Modules
{
    public class MiCakeAopCastleModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddSingleton<IMiCakeProxyProvider, CastleMiCakeProxyProvider>();
        }

        public override void Initialization(ModuleBearingContext context)
        {
            base.Initialization(context);
        }
    }
}
