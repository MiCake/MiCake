using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Bus
{
    /// <summary>
    /// A message bus. 
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Send a message to bus.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(object message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to bus with some optional headers.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="headers">some header info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default);
    }
}
