using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Provide a factory to create entity.
    /// You can pass in the required data through the constructor, and then create an entity or aggregate through the Create method
    /// </summary>
    /// <typeparam name="T"><see cref="IEntity"/></typeparam>
    public interface IFactory<T> where T : IEntity
    {
        /// <summary>
        /// Create a entity
        /// </summary>
        /// <returns></returns>
        T Create();
    }

}
