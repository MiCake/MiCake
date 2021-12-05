using MiCake.Core.Modularity;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.MessageBus.Modules
{
    public class MiCakeBusModule : MiCakeModule, IModuleSelfInspection
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            services.AddMessageBus();
        }

        public void Inspect(ModuleInspectionContext context)
        {
            var hasTransport = context.AppServiceProvider.GetService<ITransportSender>();

            if (hasTransport == null)
                throw new InvalidOperationException($"It seems that you are using message bus, " +
                    $"but you have not chosen the corresponding implementation scheme." +
                    $"Please add the corresponding support services, such as rabbitmq.");
        }
    }
}
