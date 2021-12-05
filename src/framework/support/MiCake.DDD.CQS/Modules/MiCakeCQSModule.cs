using MiCake.Core.Modularity;
using MiCake.DDD.Domain.Modules;

namespace MiCake.DDD.CQS
{
    [RelyOn(typeof(MiCakeDomainModule))]
    public class MiCakeCQSModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeCQSModule()
        {
        }
    }
}
