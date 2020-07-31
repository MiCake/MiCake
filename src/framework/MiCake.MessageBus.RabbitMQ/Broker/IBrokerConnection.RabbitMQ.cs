using MiCake.MessageBus.Broker;
using RabbitMQ.Client;
using System;

namespace MiCake.MessageBus.RabbitMQ.Broker
{
    internal class RabbitMQBrokerConnection : IBrokerConnection
    {
        public Uri EndPoint { get; private set; }

        public bool IsClosed => RabbitMQModel == null ? true : RabbitMQModel.IsClosed;

        public IModel RabbitMQModel { get; private set; }

        public RabbitMQOptions RabbitMQOptions { get; private set; }

        public RabbitMQBrokerConnection()
        {
        }

        public static RabbitMQBrokerConnection ConnectionSuccess(string hostAddress, IModel rabbitModel, RabbitMQOptions options)
            => new RabbitMQBrokerConnection()
            {
                EndPoint = new Uri(hostAddress, UriKind.RelativeOrAbsolute),
                RabbitMQModel = rabbitModel,
                RabbitMQOptions = options
            };

        public static RabbitMQBrokerConnection ConnectionFailed(string hostAddress, RabbitMQOptions options)
            => new RabbitMQBrokerConnection()
            {
                EndPoint = new Uri(hostAddress, UriKind.RelativeOrAbsolute),
                RabbitMQOptions = options
            };
    }
}
