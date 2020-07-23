using System;

namespace MiCake.Bus.Broker
{
    /// <summary>
    /// Indicates the link information for the current broker.
    /// </summary>
    public interface IBrokerConnection
    {
        /// <summary>
        /// Current endpoint info.
        /// </summary>
        Uri EndPoint { get; }

        /// <summary>
        /// Indicates current connection is closed.
        /// </summary>
        bool IsClosed { get; }
    }
}
