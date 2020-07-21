using MiCake.Bus.Serialization;
using MiCake.Bus.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus
{
    /// <summary>
    /// Default impl for <see cref="IBus"/>.
    /// </summary>
    internal class DefaultBus : IBus
    {
        private readonly ITransport _transport;
        private readonly IMessageSerializer _serializer;

        public DefaultBus(ITransport transport, IMessageSerializer messageSerializer)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _serializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        }

        public Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(object message, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
