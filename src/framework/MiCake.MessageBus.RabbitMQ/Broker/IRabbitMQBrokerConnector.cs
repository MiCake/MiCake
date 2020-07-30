using RabbitMQ.Client;
using System;

namespace MiCake.MessageBus.RabbitMQ.Broker
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// A connection channel pool to manager rabbit mq connection.
    /// 
    /// <para>
    ///     Base on CAP:[https://github.com/dotnetcore/CAP]
    /// </para>
    /// </summary>
    internal interface IRabbitMQBrokerConnector : IDisposable
    {
        /// <summary>
        /// Create a new <see cref="IModel"/>.
        /// </summary>
        /// <returns></returns>
        RabbitMQBrokerConnection CreateModel();

        /// <summary>
        /// Break appoint <see cref="IModel"/>.
        /// </summary>
        /// <param name="rabbitConnection"></param>
        bool BreakModel(RabbitMQBrokerConnection rabbitConnection);

        /// <summary>
        /// Rent a <see cref="IModel"/> to avoid unnecessary overhead by constantly creating new links.
        /// If current has no resouce,will create a new <see cref="IModel"/> and push in pool.
        /// </summary>
        /// <returns></returns>
        RabbitMQBrokerConnection RentModel();

        /// <summary>
        /// Return current <see cref="IModel"/>,order to use next time.
        /// </summary>
        /// <returns></returns>
        bool ReturnModel(RabbitMQBrokerConnection rabbitConnection);
    }
}
