using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.MessageBus.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            //User use way.
            IMessageBus bus = Mock.Of<IMessageBus>();

            //send
            await bus.SendAsync(null);

            //subscribe
            StartSubscirbe(bus);

            //will auto run background and use builder config.
            //bus.RunNewSubscriber(builder =>
            //{

            //});
        }

        private void StartSubscirbe(IMessageBus bus)
        {
            Task.Factory.StartNew(async () =>
            {
                var subscriber = await bus.CreateSubscriberAsync(new CancellationToken());
                await subscriber.SubscribeAsync();

                //subscriber.ReceivedHandlder += () =>
                //{

                //};

                subscriber.Listen();

                //use using to dispose or bus.CancelSubscriber()  ????
                await bus.CancelSubscribeAsync(subscriber);
            });
        }
    }
}
