using MiCake.Core.Util;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ
{
    /// <summary>
    /// Default impl for <see cref="IMessageSubscribeFactory"/>
    /// </summary>
    internal class RabbitMQMessageSubscribeFactory : IMessageSubscribeFactory
    {
        private readonly IRabbitMQBrokerConnector _connector;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessageSerializer _serializer;

        public RabbitMQMessageSubscribeFactory(
            IRabbitMQBrokerConnector connector,
            IMessageSerializer serializer,
            ILoggerFactory loggerFactory)
        {
            CheckValue.NotNull(connector, nameof(connector));

            _connector = connector;
            _serializer = serializer;
            _loggerFactory = loggerFactory;
        }

        public Task<IMessageSubscriber> CreateSubscriberAsync(MessageSubscriberOptions options, CancellationToken cancellationToken = default)
        {
            IMessageSubscriber result = new RabbitMQMessageSubscriber(options, _connector, _serializer, _loggerFactory);
            return Task.FromResult(result);
        }
    }
}
