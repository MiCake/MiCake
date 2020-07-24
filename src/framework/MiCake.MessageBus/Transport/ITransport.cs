using MiCake.MessageBus.Broker;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.Transport
{
    /// <summary>
    /// Defined the implementation details of a broker
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Get the transport connection info.
        /// </summary>
        IBrokerConnection Connection { get; }

        /// <summary>
        /// Ready to connect to broker.
        /// </summary>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Close current transport.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task CloseAsync(CancellationToken cancellationToken = default);
    }
}
