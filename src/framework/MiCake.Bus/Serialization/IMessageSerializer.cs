using MiCake.Bus.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus.Serialization
{
    /// <summary>
    /// A message serializer that serialize/deserialize <see cref="TransportMessage"/> and <see cref="Message"/>
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
        /// </summary>
        Task<TransportMessage> SerializeAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
        /// </summary>
        Task<Message> DeserializeAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);
    }
}
