using MiCake.MessageBus.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryMessageSubscribeFactory : IMessageSubscribeFactory
    {
        private readonly InMemoryQueue _queue;
        private readonly IMessageSerializer _serializer;

        public InMemoryMessageSubscribeFactory(InMemoryQueue queue, IMessageSerializer serializer)
        {
            _queue = queue;
            _serializer = serializer;
        }

        public Task<IMessageSubscriber> CreateSubscriberAsync(
            MessageSubscriberOptions options,
            CancellationToken cancellationToken = default)
        {
            IMessageSubscriber result = new InMemoryMessageSubscriber(_queue, _serializer);
            return Task.FromResult(result);
        }
    }
}
