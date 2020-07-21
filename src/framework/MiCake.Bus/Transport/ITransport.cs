using MiCake.Bus.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus.Transport
{
    /// <summary>
    /// The transport is responsible for sending and receiving messages
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Gets the global address of the transport's input queue
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Sends the given <see cref="TransportMessage"/> to the queue.
        /// </summary>
        /// <param name="transportMessage"><see cref="TransportMessage"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives the message from the transport's queue.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TransportMessage> Receive(CancellationToken cancellationToken = default);
    }
}
