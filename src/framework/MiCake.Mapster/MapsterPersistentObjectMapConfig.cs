using Mapster;
using MiCake.Core.Util;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;
using System.Linq.Expressions;

namespace MiCake.Mapster
{
    /// <summary>
    /// <see cref="IPersistentObjectMapConfig{TDomainEntity, TPersistentObject}"/> implement for mapster.
    /// </summary>
    internal class MapsterPersistentObjectMapConfig<TDomainEntity, TPersistentObject> : IPersistentObjectMapConfig<TDomainEntity, TPersistentObject>
          where TDomainEntity : IEntity
          where TPersistentObject : IPersistentObject
    {
        private TwoWaysTypeAdapterSetter<TDomainEntity, TPersistentObject> _mapsterConfig;

        public MapsterPersistentObjectMapConfig(TwoWaysTypeAdapterSetter<TDomainEntity, TPersistentObject> mapsterConfig)
        {
            CheckValue.NotNull(mapsterConfig, nameof(mapsterConfig));
            _mapsterConfig = mapsterConfig;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> MapProperty<TEntityProperty, TPersistentObjectProperty>(
            Expression<Func<TDomainEntity, TEntityProperty>> domainEntiyProperty,
            Expression<Func<TPersistentObject, TPersistentObjectProperty>> persistentObjectProperty)
        {
            _mapsterConfig?.Map(persistentObjectProperty, domainEntiyProperty);
            return this;
        }
    }
}
