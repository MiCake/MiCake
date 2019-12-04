using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Provides the ability for entities to create snapshots
    /// </summary>
    /// <typeparam name="TEntity"><see cref="IEntity"/></typeparam>
    public interface IEntityHasSnapshot<TEntity, TSnapshot> where TEntity : IEntity
    {
        /// <summary>
        /// Get a entity snapshot
        /// </summary>
        TSnapshot GetSnapshot();

        /// <summary>
        /// Recovering an entity from a snapshot
        /// </summary>
        /// <param name="snapshot">snapshot type</param>
        TEntity CreateEntity(TSnapshot snapshot);
    }
}
