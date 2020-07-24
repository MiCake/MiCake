using System.Collections.Generic;

namespace MiCake.MessageBus.Transport
{
    /// <summary>
    /// A options for transport when exchange mesaages.
    /// </summary>
    public class MessageExchangeOptions
    {
        /// <summary>
        /// A set of topic name.
        /// </summary>
        public IEnumerable<string> Topics { get; set; }
    }
}
