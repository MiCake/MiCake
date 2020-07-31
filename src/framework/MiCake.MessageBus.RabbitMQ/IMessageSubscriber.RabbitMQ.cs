using MiCake.Core.Util;
using MiCake.MessageBus.Helpers;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.RabbitMQ.Transport;
using MiCake.MessageBus.Serialization;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ
{
    internal class RabbitMQMessageSubscriber : IMessageSubscriber
    {
        private readonly ILogger<RabbitMQMessageSubscriber> _logger;
        private readonly IMessageSerializer _serializer;
        private RabbitMQTransportReceiver _transportReceiver;

        private SubscriberMessageReceived receivedHandlers;
        private bool isDisposed = false;

        public RabbitMQMessageSubscriber(
            MessageSubscriberOptions options,
            IRabbitMQBrokerConnector brokerConnector,
            IMessageSerializer serializer,
            ILoggerFactory loggerFactory)
        {
            CheckValue.NotNull(brokerConnector, nameof(brokerConnector));
            _transportReceiver = new RabbitMQTransportReceiver(options, brokerConnector, loggerFactory);
            _serializer = serializer;
            _logger = loggerFactory.CreateLogger<RabbitMQMessageSubscriber>();
        }

        public void AddReceivedHandler(SubscriberMessageReceived handler)
            => receivedHandlers += handler;

        public Task CommitAsync(object sender, CancellationToken cancellationToken = default)
        {
            return _transportReceiver.CompleteAsync(sender, cancellationToken);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            _transportReceiver.CloseAsync().GetAwaiter().GetResult();
            _transportReceiver = null;
        }

        public Task ListenAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"rabbit mq subscriber has started listening.");

            if (receivedHandlers != null)
            {
                _transportReceiver.OnMessageReceived += async (sender, msg) =>
                {
                    var message = await _serializer.DeserializeAsync(msg);
                    receivedHandlers?.Invoke(sender, message);
                };
            }

            return _transportReceiver.ListenAsync(cancellationToken);
        }

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            await TryConnection(_transportReceiver, cancellationToken);
            _ = _transportReceiver.SubscribeAsync(cancellationToken);
        }

        public async Task SubscribeAsync(MessageDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            if (options.Topics.Count() == 0)
                throw new ArgumentException($"{nameof(MessageDeliveryOptions.Topics)} count is zero.");

            await TryConnection(_transportReceiver, cancellationToken);

            foreach (var topic in options.Topics)
            {
                await _transportReceiver.SubscribeAsync(new MessageExchangeOptions() { Topic = topic }, cancellationToken); ;
            }
        }

        private async Task TryConnection(ITransportReceiver transportReceiver, CancellationToken cancellationToken)
        {
            if (transportReceiver.IsClose())
            {
                await transportReceiver.StartAsync(cancellationToken);
            }
        }
    }
}