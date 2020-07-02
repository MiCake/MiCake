using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;

namespace MiCake.EntityFrameworkCore.Mapping
{
    public class EFCorePoManager<TDomainEntity, TPersistentObject> : IDisposable
        where TDomainEntity : IEntity
        where TPersistentObject : IPersistentObject
    {
        private readonly IPersistentObjectReferenceMap _referenceMap = new EFCorePoReferenceMap();
        private readonly IPersistentObjectMapper _mapper;

        public EFCorePoManager(IPersistentObjectMapper persistentObjectMapper)
        {
            _mapper = persistentObjectMapper;
        }

        public void Dispose()
        {
            _referenceMap.Release();
        }

        public TDomainEntity MapToDO(TPersistentObject persistentObject)
        {
            // if(_referenceMap.TryGet()) // but i need pass into domain object.

            var domainEntity = _mapper.ToDomainEntity<TDomainEntity, TPersistentObject>(persistentObject);
            _referenceMap.AddOrUpdate(domainEntity, persistentObject);

            return domainEntity;
        }

        public TPersistentObject MapToPO(TDomainEntity domainEntity)
        {
            var persistentObj = _mapper.ToPersistentObject<TDomainEntity, TPersistentObject>(domainEntity);
            _referenceMap.AddOrUpdate(domainEntity, persistentObj);

            return persistentObj;
        }
    }
}
