using System;
using System.Collections.Generic;

namespace MiCake.MessageBus.Messages
{
    /// <summary>
    /// A message wrapper that transmitted between message bus and will be serialized into <see cref="TransportMessage"/>.
    /// </summary>
    public class Message
    {
        public Message(Dictionary<string, string> headers, object body)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets the wrapped playload object of this message
        /// </summary>
        public object Body { get; private set; }
    }
}
