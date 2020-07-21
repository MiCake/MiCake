using System;
using System.Collections.Generic;

namespace MiCake.Bus.Messages
{
    /// <summary>
    /// A message wrapper that transmitted between message bus and will be serialized into <see cref="TransportMessage"/>.
    /// </summary>
    public class Message
    {
        public Message(Dictionary<string, string> headers, object playload)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Playload = playload ?? throw new ArgumentNullException(nameof(playload));
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets the wrapped playload object of this message
        /// </summary>
        public object Playload { get; private set; }
    }
}
