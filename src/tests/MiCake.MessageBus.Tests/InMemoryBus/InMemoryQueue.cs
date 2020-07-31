using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MiCake.MessageBus.Tests.InMemoryBus
{
    internal class InMemoryQueue
    {
        private ConcurrentDictionary<string, SubscribeRecord> clientDic;

        public InMemoryQueue()
        {
            clientDic = new ConcurrentDictionary<string, SubscribeRecord>();
        }

        public void Send(byte[] message, string topic)
        {
            if (clientDic.TryGetValue(topic, out var clients))
            {
                foreach (var client in clients?.Clients)
                {
                    client.OnMessageReceived.Invoke(client, message);
                }
            }
        }

        public InMemoryConsumer Subscribe(string topic)
        {
            var consumer = new InMemoryConsumer();
            if (!clientDic.TryGetValue(topic, out var clients))
            {
                clientDic.TryAdd(topic, new SubscribeRecord()
                {
                    Topic = topic,
                    Clients = new List<InMemoryConsumer>() { consumer }
                });
            }
            else
            {
                clients.Clients.Add(consumer);
            }
            return consumer;
        }

        public void UnSubscribe(string topic)
        {
            clientDic.TryRemove(topic, out _);
        }
    }

    internal class InMemoryConsumer
    {
        public EventHandler<byte[]> OnMessageReceived { get; set; }
    }

    internal class SubscribeRecord
    {
        public string Topic { get; set; }

        public List<InMemoryConsumer> Clients { get; set; } = new List<InMemoryConsumer>();
    }
}
