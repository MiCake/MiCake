using MiCake.MessageBus.Broker;
using RabbitMQ.Client;
using System;

namespace MiCake.MessageBus.RabbitMQ.Broker
{
    internal class RabbitMQBorkerConnection : IBrokerConnection
    {
        public Uri EndPoint { get; private set; }

        public bool IsClosed => RabbitMQModel == null ? true : RabbitMQModel.IsClosed;

        public IModel RabbitMQModel { get; private set; }

        public RabbitMQBorkerConnection()
        {
        }

        public static RabbitMQBorkerConnection ConnectionSuccess(string hostAddress, IModel rabbitModel)
            => new RabbitMQBorkerConnection()
            {
                EndPoint = new Uri(hostAddress),
                RabbitMQModel = rabbitModel
            };

        public static RabbitMQBorkerConnection ConnectionFailed(string hostAddress)
            => new RabbitMQBorkerConnection()
            {
                EndPoint = new Uri(hostAddress)
            };
    }
}
