using MiCake.MessageBus.Broker;
using System;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryBrokerConnection : IBrokerConnection
    {
        public Uri EndPoint { get; set; }

        public bool IsClosed { get; set; }

        public InMemoryQueue Queue { get; set; }

        public InMemoryBrokerConnection(InMemoryQueue inMemoryQueue)
        {
            Queue = inMemoryQueue;
        }
    }
}
