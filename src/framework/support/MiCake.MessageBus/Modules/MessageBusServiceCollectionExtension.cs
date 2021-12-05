using MiCake.MessageBus;
using MiCake.MessageBus.Serialization;
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
            //use json to default serializer.
            services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

            return services;
        }
    }
}
