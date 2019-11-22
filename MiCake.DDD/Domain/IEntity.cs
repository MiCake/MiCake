using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    public interface IEntity
    {
    }

    /// <summary>
    /// Defines an entity with a single primary key with "Id" property.
    /// </summary>
    public interface IEntity<TKey> : IEntity
    {
        /// <summary>
        /// Unique identifier for this entity.
        /// </summary>
        TKey Id { get; set; }
    }
}
