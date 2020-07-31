using MiCake.MessageBus.Serialization;
using MiCake.MessageBus.Transport;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryMessageSubscriber : IMessageSubscriber
    {
        private readonly IMessageSerializer _serializer;
        private InMemoryTransportReceiver _receiver;

        private bool isDispose = false;
        private SubscriberMessageReceived handlers;

        public InMemoryMessageSubscriber(InMemoryQueue queue, IMessageSerializer serializer)
        {
            _receiver = new InMemoryTransportReceiver(queue);
            _serializer = serializer;
        }

        public void AddReceivedHandler(SubscriberMessageReceived handler)
        {
            handlers += handler;
        }

        public Task CommitAsync(object sender, CancellationToken cancellationToken = default)
        {
            return _receiver.CompleteAsync(sender);
        }

        public void Dispose()
        {
            if (isDispose)
                return;

            isDispose = true;
            _receiver.CloseAsync().GetAwaiter().GetResult();
            _receiver = null;
        }

        public Task ListenAsync(CancellationToken cancellationToken = default)
        {
            if (handlers != null)
            {
                _receiver.OnMessageReceived += async (sender, msg) =>
                {
                    var dsMes = await _serializer.DeserializeAsync(msg);
                    handlers?.Invoke(sender, dsMes);
                };
            }
            return _receiver.ListenAsync();
        }

        public Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            SubscribeAsync(new MessageDeliveryOptions() { Topics = new List<string> { "micake.test" } }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(MessageDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            foreach (var topic in options.Topics)
            {
                _receiver.SubscribeAsync(new MessageExchangeOptions() { Topic = topic });
            }
            return Task.CompletedTask;
        }
    }
}
