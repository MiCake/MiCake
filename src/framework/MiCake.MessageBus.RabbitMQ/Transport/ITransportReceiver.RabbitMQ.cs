using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ.Transport
{
    internal class RabbitMQTransportReceiver : ITransportReceiver
    {
        public IBrokerConnection Connection => throw new NotImplementedException();

        public event EventHandler<TransportMessage> OnMessageReceived;

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync(object sender, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
