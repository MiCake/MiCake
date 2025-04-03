using System.Threading.Tasks;
using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    public class BaseMiCakeModule : MiCakeModule
    {
        public override Task ConfigServices(ModuleConfigServiceContext context)
        {
            context.AutoRegisterRepositories(typeof(BaseMiCakeModule).Assembly);
            
            return base.ConfigServices(context);
        }
    }
}
