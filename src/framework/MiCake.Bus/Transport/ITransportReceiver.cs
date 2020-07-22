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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CompleteAsync(object sender, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync(CancellationToken cancellationToken = default);
    }
}
