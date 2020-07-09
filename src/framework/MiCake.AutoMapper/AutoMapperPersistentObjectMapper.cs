using AutoMapper;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;

namespace MiCake.AutoMapper
{
    /// <summary>
    /// <see cref="IPersistentObjectMapper"/> implement for AutoMapper.
    /// </summary>
    internal class AutoMapperPersistentObjectMapper : IPersistentObjectMapper
    {
        private readonly IMapper _mapper;
        public AutoMapperPersistentObjectMapper(IMapper mapper)
            => _mapper = mapper;

        public IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> Create<TDomainEntity, TPersistentObject>()
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source)
            => _mapper.Map<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination originalValue)
            => _mapper.Map(source, originalValue);

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
            => _mapper.Map<TDomainEntity>(persistentObject);

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject, TDomainEntity originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
            => _mapper.Map(persistentObject, originalSource);

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
            => _mapper.Map<TPersistentObject>(domainEntity);

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity, TPersistentObject originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject
            => _mapper.Map(domainEntity, originalSource);
    }
}
