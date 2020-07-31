using MiCake.MessageBus.Broker;
using MiCake.MessageBus.Messages;
using MiCake.MessageBus.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryTransportReceiver : ITransportReceiver
    {
        public IBrokerConnection Connection { get; private set; }

        public event EventHandler<TransportMessage> OnMessageReceived;

        private readonly InMemoryQueue _queue;
        private InMemoryConsumer _currentConsumer;
        private string subscribeTopic;

        public InMemoryTransportReceiver(InMemoryQueue queue)
        {
            _queue = queue;
            Connection = new InMemoryBrokerConnection(queue);
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            //do nothing.
            //In actually,we need release queue connection.
            _queue.UnSubscribe(subscribeTopic);
            OnMessageReceived = null;
            Connection = null;

            return Task.CompletedTask;
        }

        public Task CompleteAsync(object sender, CancellationToken cancellationToken = default)
        {
            //do nothing.
            //In actually,it's mean send ack signal to the queue.
            return Task.CompletedTask;
        }

        public Task ListenAsync(CancellationToken cancellationToken = default)
        {
            _currentConsumer.OnMessageReceived += (sender, msg) =>
            {
                OnMessageReceived.Invoke(sender, new TransportMessage(new Dictionary<string, string>(), msg));
            };

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            //do nothing.
            //In actually,we need connect to the queue middleware.
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(new MessageExchangeOptions() { Topic = "micake.test" }, cancellationToken);
        }

        public Task SubscribeAsync(MessageExchangeOptions options, CancellationToken cancellationToken = default)
        {
            _currentConsumer = _queue.Subscribe(options.Topic);
            subscribeTopic = options.Topic;
            return Task.CompletedTask;
        }
    }
}
