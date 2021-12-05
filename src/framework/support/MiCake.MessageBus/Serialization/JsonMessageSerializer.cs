using MiCake.Core.Util;
using MiCake.MessageBus.Messages;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Serialization
{
    /// <summary>
    /// A <see cref="IMessageSerializer"/> use json and utf-8.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        public Task<Message> DeserializeAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(transportMessage, nameof(transportMessage));

            var bodyObject = JsonSerializer.Deserialize<object>(Encoding.UTF8.GetString(transportMessage.Body));
            return Task.FromResult(new Message(transportMessage.Headers, bodyObject));
        }

        public Task<TransportMessage> SerializeAsync(Message message, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(message, nameof(message));

            var jsonBody = JsonSerializer.Serialize(message.Body);
            return Task.FromResult(new TransportMessage(message.Headers, Encoding.UTF8.GetBytes(jsonBody)));
        }
    }
}
