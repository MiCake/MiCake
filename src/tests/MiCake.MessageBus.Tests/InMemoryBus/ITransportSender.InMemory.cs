using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.Transport;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryTransportSender : ITransportSender
    {
        public IBrokerConnection Connection { get; private set; }

        private readonly InMemoryQueue _queue;
        public InMemoryTransportSender(InMemoryQueue queue)
        {
            _queue = queue;
            Connection = new InMemoryBrokerConnection(queue);
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            //do nothing.
            //In actually,we need release queue connection.
            return Task.CompletedTask;
        }

        public Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
        {
            return SendAsync(transportMessage, new MessageExchangeOptions() { Topic = "micake.test" }, cancellationToken);
        }

        public Task SendAsync(TransportMessage transportMessage, MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            _queue.Send(transportMessage.Body, options.Topic);
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            //do nothing.
            //In actually,we need connect to the queue middleware.
            return Task.CompletedTask;
        }
    }
}
