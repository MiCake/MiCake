using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.RabbitMQ.Broker;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ.Transport
{
    internal class RabbitMQTransportReceiver : ITransportReceiver
    {
        IBrokerConnection ITransport.Connection => RabbitConnection;

        private RabbitMQBrokerConnection RabbitConnection;
        private readonly IRabbitMQBrokerConnector _brokerConnector;
        private readonly ILogger<RabbitMQTransportReceiver> _logger;
        private readonly MessageSubscriberOptions _subscriberOptios;

        public event EventHandler<TransportMessage> OnMessageReceived;

        private bool isClosed = false;

        public RabbitMQTransportReceiver(
            MessageSubscriberOptions options,
            IRabbitMQBrokerConnector brokerConnector,
            ILoggerFactory loggerFactory)
        {
            _subscriberOptios = options;
            _brokerConnector = brokerConnector;
            _logger = loggerFactory.CreateLogger<RabbitMQTransportReceiver>();
        }

        public Task CompleteAsync(object sender, CancellationToken cancellationToken = default)
        {
            var rbtModel = CheckConnection(RabbitConnection).RabbitMQModel;
            rbtModel.BasicAck((ulong)sender, false);

            return Task.CompletedTask;
        }

        public Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            return SubscribeInternal(RabbitConnection.RabbitMQOptions.Topic, cancellationToken);
        }

        public Task SubscribeAsync(MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            return SubscribeInternal(options.Topic, cancellationToken);
        }

        private Task SubscribeInternal(string topic, CancellationToken cancellationToken = default)
        {
            var rbtModel = CheckConnection(RabbitConnection).RabbitMQModel;

            rbtModel.QueueBind(_subscriberOptios.SubscriptionName, RabbitConnection.RabbitMQOptions.ExchangeName, topic);

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            RabbitConnection ??= _brokerConnector.CreateModel();

            var rabbitModel = RabbitConnection.RabbitMQModel;
            var rbtOptions = RabbitConnection.RabbitMQOptions;

            rabbitModel.ExchangeDeclare(rbtOptions.ExchangeName, RabbitMQOptions.ExchangeType, true);

            var arguments = new Dictionary<string, object>
            {
                {"x-message-ttl", rbtOptions.QueueMessageExpires}
            };
            rabbitModel.QueueDeclare(_subscriberOptios.SubscriptionName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);

            _logger.LogInformation($"A new {nameof(RabbitMQTransportReceiver)} has started.");

            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (isClosed)
                return Task.CompletedTask;

            isClosed = true;

            if (RabbitConnection == null)
                return Task.CompletedTask;

            var breakResult = _brokerConnector.BreakModel(RabbitConnection);

            if (!breakResult && !RabbitConnection.IsClosed)
                RabbitConnection.RabbitMQModel.Dispose();

            RabbitConnection = null;

            return Task.CompletedTask;
        }

        private RabbitMQBrokerConnection CheckConnection(RabbitMQBrokerConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException($"Current connection is null.Please make sure rabbit mq is running and { nameof(RabbitMQTransportReceiver) } is { nameof(StartAsync)}");

            return connection;
        }

        public Task ListenAsync(CancellationToken cancellationToken = default)
        {
            var rbtModel = CheckConnection(RabbitConnection).RabbitMQModel;

            var consumer = new EventingBasicConsumer(rbtModel);
            //register received event.
            consumer.Received += OnConsumerReceived;
            rbtModel.BasicConsume("", false, consumer);

            return Task.CompletedTask;
        }

        #region events
        private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in e.BasicProperties.Headers)
            {
                headers.Add(header.Key, header.Value == null ? null : Encoding.UTF8.GetString((byte[])header.Value));
            }

            var message = new TransportMessage(headers, e.Body.ToArray());

            OnMessageReceived?.Invoke(e.DeliveryTag, message);
        }
        #endregion
    }
}
