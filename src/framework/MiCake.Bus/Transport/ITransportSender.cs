using MiCake.Bus.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus.Transport
{
    /// <summary>
    /// The transport is responsible for sending message.
    /// </summary>
    public interface ITransportSender : ITransport
    {
        ///  <summary>
        /// A event handler when message sended.
        /// </summary>
        event EventHandler<TransportMessage> OnMessageSended;

        /// <summary>
        /// Sends the given <see cref="TransportMessage"/> to the queue.
        /// </summary>
        /// <param name="transportMessage"><see cref="TransportMessage"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);
    }
}
