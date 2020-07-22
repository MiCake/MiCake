using System;

namespace MiCake.Bus.Broker
{
    /// <summary>
    /// 
    /// </summary>
    public class BrokerConnection
    {
        /// <summary>
        /// 
        /// </summary>
        public Uri EndPoint { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsClosed { get; private set; }
    }
}
