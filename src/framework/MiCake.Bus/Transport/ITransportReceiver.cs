using MiCake.Bus.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus.Transport
{
    /// <summary>
    /// The transport is responsible for receiving message.
    /// </summary>
    public interface ITransportReceiver
    {
        /// <summary>
        /// A event handler when message received.
        /// </summary>
        event EventHandler<TransportMessage> OnMessageReceived;

        /// <summary>
        /// Indicates that the message has been received and processed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CompleteAsync(object sender, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to the message queue by global config.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to the message queue by appoint config.
        /// </summary>
        /// <param name="options"><see cref="TransportSubscribeOptions"/></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync(TransportSubscribeOptions options, CancellationToken cancellationToken = default);
    }
}
