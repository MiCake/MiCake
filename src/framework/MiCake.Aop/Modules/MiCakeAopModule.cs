using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Aop.Modules
{
    public class MiCakeAopModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddTransient(provider =>
            {
                return provider.GetService<IMiCakeProxyProvider>().GetMiCakeProxy();
            });
        }
    }
}
