using MiCake;
using MiCake.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus.ConsoleDemo
{
    public class RabbitMQMessageBusTest
    {
        private readonly List<IMessageSubscriber> subscribers = new();
        public RabbitMQMessageBusTest()
        {

        }

        public async Task Test(CancellationToken cancellationToken, int sendInterval = 100)
        {
            using (var scope = CreateServices().CreateScope())
            {
                var provider = scope.ServiceProvider;
                var bus = provider.GetService<IMessageBus>();

                await CreateSubscribe(bus);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await bus.PublishAsync(Guid.NewGuid().ToString(), cancellationToken);

                    Thread.Sleep(sendInterval);
                }

                foreach (var item in subscribers)
                {
                    await bus.CancelSubscribeAsync(item);
                }
            };
        }

        public async Task TestWithMoreSubscriber(CancellationToken cancellationToken, int clientCount, int sendInterval = 100)
        {
            using (var scope = CreateServices().CreateScope())
            {
                var provider = scope.ServiceProvider;
                var bus = provider.GetService<IMessageBus>();

                for (int i = 0; i < clientCount; i++)
                {
                    await CreateSubscribe(bus, $"client{i}");
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    await bus.PublishAsync(Guid.NewGuid().ToString(), cancellationToken);

                    Thread.Sleep(sendInterval);
                }

                foreach (var item in subscribers)
                {
                    await bus.CancelSubscribeAsync(item);
                }
            };
        }

        private async Task CreateSubscribe(IMessageBus bus, string name = "client1")
        {
            //create subscriber
            var subscriber = await bus.CreateSubscriberAsync(new MessageSubscriberOptions() { SubscriptionName = "micake.test" });
            await subscriber.SubscribeAsync();
            await subscriber.AddReceivedHandlerAsync(async (sender, msg) =>
             {
                 Console.WriteLine($"{name} --- Received msg:{msg.Body}.");
                 await subscriber.CommitAsync(sender);
             });
            await subscriber.ListenAsync();

            subscribers.Add(subscriber);
        }

        public IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(config =>
            {
                config.SetMinimumLevel(LogLevel.Trace);
            });
            services.Configure<ILoggingBuilder>(builder =>
            {
                builder.AddConsole(options =>
                {
                });
            });
            services.AddMessageBus();
            services.AddRabbitMQ(options =>
            {
            });

            return services.BuildServiceProvider();
        }
    }
}
