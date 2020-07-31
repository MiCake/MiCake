using MiCake.MessageBus.Tests.InMemoryBus;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.MessageBus.Tests
{
    internal static class InMemoryBusServiceExtension
    {
        public static IServiceCollection AddInMemoryBus(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddMessageBus();

            services.TryAddSingleton<InMemoryQueue>();
            services.TryAddSingleton<ITransportSender, InMemoryTransportSender>();
            services.TryAddSingleton<IMessageSubscribeFactory, InMemoryMessageSubscribeFactory>();

            return services;
        }
    }
}
