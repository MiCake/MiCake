using System.Collections.Generic;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Describes the configuration where messages are delivered.
    /// </summary>
    public class MessageDeliveryOptions
    {
        /// <summary>
        /// A set of topic name.
        /// </summary>
        public IEnumerable<string> Topics { get; set; }
    }
}
