using AutoMapper;
using MiCake.Core.Util;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;

namespace MiCake.AutoMapper
{
    /// <summary>
    /// <see cref="IPersistentObjectMapper"/> implement for AutoMapper.
    /// </summary>
    internal class AutoMapperPersistentObjectMapper : IPersistentObjectMapper
    {
        private IMapperConfigurationExpression _mapperConfiguration;

        private readonly IMapper _mapper;

        public bool InitAtStartup => false;

        public AutoMapperPersistentObjectMapper(IMapper mapper)
            => _mapper = mapper;

        public IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> Create<TKey, TDomainEntity, TPersistentObject>()
            where TDomainEntity : IAggregateRoot<TKey>
            where TPersistentObject : IPersistentObject<TKey, TDomainEntity>
        {
            CheckValue.NotNull(_mapperConfiguration, "AutoMapper:IMapperConfigurationExpression");

            var autoMapperExpression = _mapperConfiguration.CreateMap<TDomainEntity, TPersistentObject>();
            return new AutoMapperPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject>(autoMapperExpression);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
            => _mapper.Map<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination originalValue)
            => _mapper.Map(source, originalValue);

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject)
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject
            => _mapper.Map<TDomainEntity>(persistentObject);

        public TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject, TDomainEntity originalSource)
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject
            => _mapper.Map(persistentObject, originalSource);

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity)
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject
            => _mapper.Map<TPersistentObject>(domainEntity);

        public TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity, TPersistentObject originalSource)
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject
            => _mapper.Map(domainEntity, originalSource);

        internal void SetAutoMapperConfigExpression(IMapperConfigurationExpression mapperConfiguration)
            => _mapperConfiguration = mapperConfiguration;
    }
}
