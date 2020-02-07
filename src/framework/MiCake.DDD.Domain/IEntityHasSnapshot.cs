using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Provides the ability for entities to create snapshots
    /// </summary>
    /// <typeparam name="TEntity"><see cref="IEntity"/></typeparam>
    public interface IEntityHasSnapshot<TSnapshot> : IEntityHasSnapshot
    {
        /// <summary>
        /// Get a entity snapshot
        /// </summary>
        TSnapshot GetSnapshot();
    }

    /// <summary>
    /// this is base interface.
    /// you can use generic interface <see cref="IEntityHasSnapshot{TSnapshot}"/>
    /// </summary>
    public interface IEntityHasSnapshot
    {
    }
}
