using MiCake.Core.Util;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.Serialization;
using MiCake.MessageBus.Transport;
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
        private readonly ITransportSender _transport;
        private readonly IMessageSerializer _serializer;

        public DefaultMessageBus(ITransportSender transport, IMessageSerializer messageSerializer)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _serializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        }

        public Task CancelSubscribeAsync(IMessageSubscriber messageSubscriber)
        {
            throw new NotImplementedException();
        }

        public Task<IMessageSubscriber> CreateSubscriberAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public virtual Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            return SendAsync(message, new Dictionary<string, string>(), cancellationToken);
        }

        public virtual async Task SendAsync(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(message, nameof(message));

            if (_transport.Connection.IsClosed)
            {
                throw new InvalidOperationException($"Current broker has already closed,Please make sure broker is running.");
            }

            var transportMsg = await _serializer.SerializeAsync(new Message(headers, message));
            await _transport.SendAsync(transportMsg, cancellationToken);
        }

        public Task SendAsync(object message, Dictionary<string, string> headers, MessageDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}