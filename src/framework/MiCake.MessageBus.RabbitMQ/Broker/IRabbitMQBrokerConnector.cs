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
        RabbitMQBorkerConnection CreateModel();

        /// <summary>
        /// Break appoint <see cref="IModel"/>.
        /// </summary>
        /// <param name="rabbitConnection"></param>
        bool BreakModel(RabbitMQBorkerConnection rabbitConnection);

        /// <summary>
        /// Rent a <see cref="IModel"/> to avoid unnecessary overhead by constantly creating new links.
        /// If current has no resouce,will create a new <see cref="IModel"/> and push in pool.
        /// </summary>
        /// <returns></returns>
        RabbitMQBorkerConnection RentModel();

        /// <summary>
        /// Return current <see cref="IModel"/>,easy to use next time
        /// </summary>
        /// <returns></returns>
        bool ReturnModel(RabbitMQBorkerConnection rabbitConnection);
    }
}
