using MiCake.Bus.Messages;
using MiCake.Bus.Serialization;
using MiCake.Bus.Transport;
using MiCake.Core.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus
{
    /// <summary>
    /// Default impl for <see cref="IBus"/>.
    /// </summary>
    public class DefaultBus : IBus
    {
        private readonly ITransportSender _transport;
        private readonly IMessageSerializer _serializer;

        public DefaultBus(ITransportSender transport, IMessageSerializer messageSerializer)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _serializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
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
    }
}