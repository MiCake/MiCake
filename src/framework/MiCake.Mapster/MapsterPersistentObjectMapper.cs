using Mapster;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;

namespace MiCake.Mapster
{
    /// <summary>
    /// <see cref="IPersistentObjectMapper"/> implement for mapster.
    /// </summary>
    internal class MapsterPersistentObjectMapper : IPersistentObjectMapper
    {
        public MapsterPersistentObjectMapper()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> Create<TDomainEntity, TPersistentObject>()
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            var result = TypeAdapterConfig<TDomainEntity, TPersistentObject>.NewConfig()
                                      .MapDomainEvent()
                                      .TwoWays();

            return new MapsterPersistentObjectMapConfig<TDomainEntity, TPersistentObject>(result);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return source.Adapt<TDestination>();
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination originalValue)
        {
            return source.Adapt(originalValue);
        }

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            return persistentObject.Adapt<TDomainEntity>();
        }

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject, TDomainEntity originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            return persistentObject.Adapt(originalSource);
        }

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            return domainEntity.Adapt<TPersistentObject>();
        }

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity, TPersistentObject originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            return domainEntity.Adapt(originalSource);
        }
    }
}
