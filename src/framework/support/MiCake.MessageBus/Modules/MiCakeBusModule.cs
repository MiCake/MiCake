using MiCake.Core.Modularity;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.MessageBus.Modules
{
    public class MiCakeBusModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            services.AddMessageBus();
        }

        public override void Initialization(ModuleLoadContext context)
        {
            var hasTransport = context.ServiceProvider.GetService<ITransportSender>();
            if (hasTransport == null)
                throw new InvalidOperationException($"It seems that you are using message bus, " +
                    $"but you have not chosen the corresponding implementation scheme." +
                    $"Please add the corresponding support services, such as rabbitmq.");

            base.Initialization(context);
        }
    }
}
