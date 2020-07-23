using System.Collections.Generic;

namespace MiCake.Bus.Transport
{
    /// <summary>
    /// A options for subscribe to the message queue.
    /// </summary>
    public class TransportSubscribeOptions
    {
        /// <summary>
        /// A set of topic name.
        /// </summary>
        public IEnumerable<string> Topics { get; set; }
    }
}
