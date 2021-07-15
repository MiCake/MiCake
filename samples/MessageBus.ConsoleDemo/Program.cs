using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus.ConsoleDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource cts = new();

            _ = Task.Factory.StartNew(() =>
              {
                  RabbitMQMessageBusTest testCase = new();
                  _ = testCase.TestWithMoreSubscriber(cts.Token, 5, 3);
              });

            Console.WriteLine("Enter any to stop..");
            Console.ReadLine();
            cts.Cancel();
            Console.WriteLine("Stop.Please see rabbit mq ui has any connection?.");
            Console.ReadLine();
        }
    }
}
