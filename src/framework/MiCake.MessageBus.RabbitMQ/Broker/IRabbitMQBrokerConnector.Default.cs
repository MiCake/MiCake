using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace MiCake.MessageBus.RabbitMQ.Broker
{
    /// <summary>
    /// Default impl for <see cref="IRabbitMQBrokerConnector"/>
    /// </summary>
    internal class RabbitMQBrokerConnector : IRabbitMQBrokerConnector
    {
        private readonly Func<IConnection> _connectionActivator;
        private readonly ILogger<RabbitMQBrokerConnector> _logger;
        private readonly ConcurrentQueue<IModel> _pool;
        private readonly RabbitMQOptions _options;
        private IConnection _connection;
        private static readonly object SLock = new object();

        private int _count;
        private int _maxSize;

        public RabbitMQBrokerConnector(
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQBrokerConnector> logger)
        {
            _options = options.Value;
            _logger = logger;

            _maxSize = _options.KeepConnectionNumber;
            _pool = new ConcurrentQueue<IModel>();
            _connectionActivator = CreateConnection(_options);
        }

        public RabbitMQBrokerConnection CreateModel()
        {
            try
            {
                var model = GetConnection().CreateModel();
                return RabbitMQBrokerConnection.ConnectionSuccess(_options.HostName, model, _options);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQ channel model create failed!");
                Console.WriteLine(e);
                throw;
            }
        }

        public bool BreakModel(RabbitMQBrokerConnection rabbitConnection)
        {
            if (rabbitConnection.IsClosed)
                return true;

            try
            {
                var rbtModel = rabbitConnection.RabbitMQModel;
                rbtModel.Close();
                rbtModel.Dispose();

                return true;
            }
            catch
            {
                return rabbitConnection.IsClosed;
            }
        }

        public RabbitMQBrokerConnection RentModel()
        {
            lock (SLock)
            {
                while (_count > _maxSize)
                {
                    Thread.SpinWait(1);
                }
                return RentModelInternal();
            }
        }

        private RabbitMQBrokerConnection RentModelInternal()
        {
            if (_pool.TryDequeue(out var model))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                return RabbitMQBrokerConnection.ConnectionSuccess(_options.HostName, model, _options);
            }

            try
            {
                model = GetConnection().CreateModel();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQ channel model create failed!");
                Console.WriteLine(e);
                throw;
            }

            return RabbitMQBrokerConnection.ConnectionSuccess(_options.HostName, model, _options);
        }

        public bool ReturnModel(RabbitMQBrokerConnection rabbitConnection)
        {
            var rbtModel = rabbitConnection.RabbitMQModel;
            if (Interlocked.Increment(ref _count) <= _maxSize && rbtModel.IsOpen)
            {
                _pool.Enqueue(rbtModel);

                return true;
            }

            Interlocked.Decrement(ref _count);

            Debug.Assert(_maxSize == 0 || _pool.Count <= _maxSize);

            return false;
        }

        public IConnection GetConnection()
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            _connection = _connectionActivator();
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
            return _connection;
        }

        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning($"RabbitMQ client connection closed! --> {e.ReplyText}");
        }

        private static Func<IConnection> CreateConnection(RabbitMQOptions options)
        {
            var serviceName = Assembly.GetEntryAssembly()?.GetName().Name.ToLower();

            var factory = new ConnectionFactory
            {
                UserName = options.UserName,
                Port = options.Port,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };

            if (options.HostName.Contains(","))
            {
                options.ConnectionFactoryOptions?.Invoke(factory);
                return () => factory.CreateConnection(
                    options.HostName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries), serviceName);
            }

            factory.HostName = options.HostName;
            options.ConnectionFactoryOptions?.Invoke(factory);
            return () => factory.CreateConnection(serviceName);
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
            {
                context.Dispose();
            }
            _connection?.Dispose();
        }
    }
}
