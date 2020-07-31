using MiCake.MessageBus.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Transport
{
    /// <summary>
    /// The transport is responsible for sending message.
    /// </summary>
    public interface ITransportSender : ITransport
    {
        /// <summary>
        /// Sends the given <see cref="TransportMessage"/> to the queue.
        /// </summary>
        /// <param name="transportMessage"><see cref="TransportMessage"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the given <see cref="TransportMessage"/> to the queue with appoint options.
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendAsync(TransportMessage transportMessage, MessageExchangeOptions options, CancellationToken cancellationToken = default);
    }
}
