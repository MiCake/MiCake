using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Provide a factory to create complex entity(aggregate) or value object.
    /// </summary>
    /// <typeparam name="T"><see cref="IEntity"/></typeparam>
    public interface IFactory<T>
    {
        /// <summary>
        /// Create a entity
        /// </summary>
        /// <returns></returns>
        T Create();
    }

}
