using MiCake.Core.Modularity;

namespace MiCake.Identity.Modules
{
    [CoreModule]
    public class MiCakeIdentityModule : MiCakeModule
    {
        public const string CurrentIdentityUserKeyType = "MiCake.Identity.User.KeyType";

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }
    }
}
