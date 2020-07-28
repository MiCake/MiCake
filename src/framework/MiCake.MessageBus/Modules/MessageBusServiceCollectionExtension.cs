using MiCake.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake
{
    public static class MessageBusServiceCollectionExtension
    {
        public static IServiceCollection AddMessageBus(this IServiceCollection services)
        {
            services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
            services.TryAddSingleton<ISubscribeManager, DefaultSubscribeManager>();

            return services;
        }
    }
}
