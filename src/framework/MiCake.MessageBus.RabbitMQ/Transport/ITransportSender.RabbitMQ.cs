using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ.Transport
{
    /// <summary>
    /// Rabbit MQ impl for <see cref="ITransportSender"/>
    /// 
    /// For the sender, it does not need to open multiple links, so it uses reusable "Rent" method.
    /// </summary>
    internal class RabbitMQTransportSender : ITransportSender
    {
        IBrokerConnection ITransport.Connection => RabbitConnection;

        private RabbitMQBrokerConnection RabbitConnection;
        private readonly IRabbitMQBrokerConnector _brokerConnector;
        private readonly ILogger<RabbitMQTransportSender> _logger;

        private bool isClosed;

        public RabbitMQTransportSender(
            IRabbitMQBrokerConnector brokerConnector,
            ILoggerFactory loggerFactory)
        {
            _brokerConnector = brokerConnector;
            _logger = loggerFactory.CreateLogger<RabbitMQTransportSender>();
        }

        public Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
        {
            return SendInternal(transportMessage, RabbitConnection.RabbitMQOptions.Topic, cancellationToken);
        }

        public Task SendAsync(TransportMessage transportMessage, MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            return SendInternal(transportMessage, options.Topic, cancellationToken);
        }

        //The error is not captured here, but is captured by the upper message bus.
        private Task SendInternal(TransportMessage transportMessage, string routerKey, CancellationToken cancellationToken)
        {
            if (RabbitConnection == null)
                throw new ArgumentNullException($"Current connection is null.Please make sure rabbit mq is running and {nameof(RabbitMQTransportSender)} is {nameof(StartAsync)}");

            var exchange = RabbitConnection.RabbitMQOptions.ExchangeName;
            var channel = RabbitConnection.RabbitMQModel;

            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.Headers = transportMessage.Headers.ToDictionary(x => x.Key, x => (object)x.Value);

            channel.ExchangeDeclare(exchange, RabbitMQOptions.ExchangeType, true);
            channel.BasicPublish(exchange, routerKey, props, transportMessage.Body);

            _logger.LogDebug($"RabbitMQ topic message [{routerKey}] has been published.");

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            RabbitConnection ??= _brokerConnector.RentModel();

            _logger.LogInformation($"{nameof(RabbitMQTransportSender)} has started.");

            return Task.CompletedTask;
        }

        //For the sender,close is mean "return" current connection to pool rather than release rabbit mq connection.
        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (isClosed)
                return Task.CompletedTask;

            isClosed = true;

            if (RabbitConnection == null)
                return Task.CompletedTask;

            var returnResult = _brokerConnector.ReturnModel(RabbitConnection);
            //if return error,release IModel manually.
            if (!returnResult && !RabbitConnection.IsClosed)
            {
                RabbitConnection.RabbitMQModel.Dispose();
            }
            RabbitConnection = null;

            return Task.CompletedTask;
        }
    }
}
