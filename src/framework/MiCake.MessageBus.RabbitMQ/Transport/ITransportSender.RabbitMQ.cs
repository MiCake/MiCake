using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ.Transport
{
    internal class RabbitMQTransportSender : ITransportSender
    {
        public IBrokerConnection Connection => throw new NotImplementedException();

        private readonly IRabbitMQBrokerConnector _brokerConnector;
        private readonly ILogger<RabbitMQTransportSender> _logger;
        private readonly string _exchange;

        public RabbitMQTransportSender(
            IRabbitMQBrokerConnector brokerConnector,
            ILogger<RabbitMQTransportSender> logger)
        {
            _brokerConnector = brokerConnector;
            _logger = logger;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(TransportMessage transportMessage, MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            var rbtModel = _brokerConnector.RentModel();

            return Task.CompletedTask;
        }
    }
}
