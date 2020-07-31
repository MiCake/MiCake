using MiCake.MessageBus;
using MiCake.MessageBus.RabbitMQ;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.RabbitMQ.Transport;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace MiCake
{
    public static class RabbitMQServiceCollectionExtension
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services)
        {
            return AddRabbitMQ(services, null);
        }

        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<RabbitMQOptions> optionsAction)
        {
            RabbitMQOptions options = new RabbitMQOptions();

            services.Configure(optionsAction);
            services.TryAddSingleton<ITransportSender, RabbitMQTransportSender>();
            services.TryAddSingleton<IMessageSubscribeFactory, RabbitMQMessageSubscribeFactory>();
            services.TryAddSingleton<IRabbitMQBrokerConnector, RabbitMQBrokerConnector>();

            return services;
        }
    }
}
