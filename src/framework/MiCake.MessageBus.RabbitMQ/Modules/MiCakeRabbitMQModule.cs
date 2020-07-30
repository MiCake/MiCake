using MiCake.Core.Modularity;
using MiCake.MessageBus.Modules;

namespace MiCake.MessageBus.RabbitMQ.Modules
{
    [RelyOn(typeof(MiCakeBusModule))]
    public class MiCakeRabbitMQModule : MiCakeModule
    {
    }
}
