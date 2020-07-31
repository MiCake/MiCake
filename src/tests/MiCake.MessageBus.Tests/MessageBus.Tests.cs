using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.MessageBus.Tests
{
    public class MessageBusTests
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        protected IMessageBus MessageBus { get; private set; }

        public MessageBusTests()
        {
            var services = new ServiceCollection();

            services.AddInMemoryBus();
            ServiceProvider = services.BuildServiceProvider();
            MessageBus = ServiceProvider.GetService<IMessageBus>();
        }

        [Fact]
        public async Task Bus_SendMessage()
        {
            var headers = new Dictionary<string, string>()
            {
                { "test","new value."}
            };
            await MessageBus.PublishAsync("hello");
            await MessageBus.PublishAsync("hello", headers);
            await MessageBus.PublishAsync("hello", headers, new MessageDeliveryOptions() { Topics = new List<string>() { "new-topic" } });
        }

        [Fact]
        public async Task Bus_CreateSubscriber()
        {
            var subscriber = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            Assert.NotNull(subscriber);
        }

        [Fact]
        public async Task Bus_MoreSubscriberIsDifferentInstance()
        {
            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            var subscriber2 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            Assert.NotNull(subscriber1);
            Assert.NotNull(subscriber2);
            Assert.NotSame(subscriber1, subscriber2);
        }

        [Fact]
        public async Task Bus_MessageReceived()
        {
            string result = string.Empty;

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result = d.Body.ToString();
            });
            _ = subscriber1.ListenAsync();

            await MessageBus.PublishAsync("hello");

            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task Bus_PublishMoreTimes()
        {
            int result = 0;

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result += int.Parse(d.Body.ToString());
            });
            _ = subscriber1.ListenAsync();

            await MessageBus.PublishAsync(1);
            await MessageBus.PublishAsync(2);
            await MessageBus.PublishAsync(3);
            await MessageBus.PublishAsync(4);

            Assert.Equal(10, result);
        }


        [Fact]
        public async Task Bus_MessageReceived_AddMoreHandler()
        {
            StringBuilder result = new StringBuilder();

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });

            subscriber1.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });


            _ = subscriber1.ListenAsync();

            await MessageBus.PublishAsync("hello");

            Assert.Equal("hellohello", result.ToString());
        }

        [Fact]
        public async Task Bus_MoreMessageReceiver()
        {
            StringBuilder result = new StringBuilder();

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });
            _ = subscriber1.ListenAsync();

            var subscriber2 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            await subscriber2.SubscribeAsync();
            subscriber2.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });
            _ = subscriber2.ListenAsync();

            await MessageBus.PublishAsync("hello");

            Assert.Equal("hellohello", result.ToString());
        }

        [Fact]
        public async Task Bus_MoreMessageReceiver_DifferentTopic()
        {
            StringBuilder result = new StringBuilder();

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            await subscriber1.SubscribeAsync(new MessageDeliveryOptions() { Topics = new List<string>() { "topic1" } });
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });
            _ = subscriber1.ListenAsync();

            var subscriber2 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());
            await subscriber2.SubscribeAsync(new MessageDeliveryOptions() { Topics = new List<string>() { "topic2" } });
            subscriber2.AddReceivedHandler((s, d) =>
            {
                result.Append(d.Body.ToString());
            });
            _ = subscriber2.ListenAsync();

            await MessageBus.PublishAsync("hello", new Dictionary<string, string>(), new MessageDeliveryOptions() { Topics = new List<string>() { "topic1" } });
            Assert.Equal("hello", result.ToString());

            await MessageBus.PublishAsync("micake", new Dictionary<string, string>(), new MessageDeliveryOptions() { Topics = new List<string>() { "topic2" } });
            Assert.Equal("hellomicake", result.ToString());
        }

        [Fact]
        public async Task Bus_CancelMessageReceiver()
        {
            string result = string.Empty;

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result = d.Body.ToString();
            });
            _ = subscriber1.ListenAsync();
            await MessageBus.CancelSubscribeAsync(subscriber1);

            await MessageBus.PublishAsync("hello");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task Bus_CancelMessageReceiver_ReuseWillError()
        {
            string result = string.Empty;

            var subscriber1 = await MessageBus.CreateSubscriberAsync(new MessageSubscriberOptions());

            await subscriber1.SubscribeAsync();
            subscriber1.AddReceivedHandler((s, d) =>
            {
                result = d.Body.ToString();
            });
            _ = subscriber1.ListenAsync();
            await MessageBus.CancelSubscribeAsync(subscriber1);

            await MessageBus.PublishAsync("hello");

            //reuse
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await subscriber1.SubscribeAsync();
                subscriber1.AddReceivedHandler((s, d) =>
                {
                    result = d.Body.ToString();
                });
                _ = subscriber1.ListenAsync();
            });

            await MessageBus.PublishAsync("hello");

            Assert.Equal(string.Empty, result);
        }
    }
}