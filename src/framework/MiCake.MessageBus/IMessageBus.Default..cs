using MiCake.Core.Util;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.Serialization;
using MiCake.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Default impl for <see cref="IMessageBus"/>.
    /// </summary>
    public class DefaultMessageBus : IMessageBus
    {
        private readonly ILogger<IMessageBus> _logger;
        private readonly ITransportSender _transport;
        private readonly IMessageSerializer _serializer;
        private readonly ISubscribeManager _subscribeManager;

        public DefaultMessageBus(
            ITransportSender transport,
            IMessageSerializer messageSerializer,
            ISubscribeManager subscribeManager,
            ILoggerFactory loggerFactory
            )
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(ITransportSender));
            _serializer = messageSerializer ?? throw new ArgumentNullException(nameof(IMessageSerializer));
            _subscribeManager = subscribeManager ?? throw new ArgumentNullException(nameof(ISubscribeManager));
            _logger = loggerFactory.CreateLogger<IMessageBus>();
        }

        public Task CancelSubscribeAsync(IMessageSubscriber messageSubscriber)
        {
            _logger.LogInformation($"Message Bus cancel a subscriber.");

            return _subscribeManager.RemoveAsync(messageSubscriber);
        }

        public Task<IMessageSubscriber> CreateSubscriberAsync(CancellationTokenSource cancellationTokenSource)
        {
            _logger.LogInformation($"Message Bus create a subscriber.");

            return _subscribeManager.CreateAsync(cancellationTokenSource);
        }

        public virtual Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            return SendAsync(message, new Dictionary<string, string>(), cancellationToken);
        }

        public virtual async Task SendAsync(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            CheckMessage(message);

            var transportMsg = await _serializer.SerializeAsync(new Message(headers, message));
            await _transport.SendAsync(transportMsg, cancellationToken);
        }

        public virtual async Task SendAsync(object message, Dictionary<string, string> headers, MessageDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            CheckMessage(message);

            var transportMsg = await _serializer.SerializeAsync(new Message(headers, message));
            await _transport.SendAsync(transportMsg, ConvertMessageOptions(options), cancellationToken);
        }

        private MessageExchangeOptions ConvertMessageOptions(MessageDeliveryOptions deliveryOptions)
        {
            return new MessageExchangeOptions()
            {
                Topics = deliveryOptions.Topics
            };
        }

        private void CheckMessage(object message)
        {
            CheckValue.NotNull(message, nameof(message));

            if (_transport.Connection.IsClosed)
            {
                throw new InvalidOperationException($"Current broker has already closed,Please make sure broker is running.");
            }
        }
    }
}