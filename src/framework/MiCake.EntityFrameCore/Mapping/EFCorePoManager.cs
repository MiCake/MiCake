using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Mapping
{
    public class EFCorePoManager<TDomainEntity, TPersistentObject> : IDisposable
        where TDomainEntity : IAggregateRoot
        where TPersistentObject : IPersistentObject
    {
        private IPersistentObjectReferenceMap _referenceMap = new EFCorePoReferenceMap();
        private IPersistentObjectMapper _mapper;

        private bool isDispose;

        public EFCorePoManager(IPersistentObjectMapper persistentObjectMapper)
        {
            _mapper = persistentObjectMapper;
        }

        public void Dispose()
        {
            if (isDispose)
                return;

            _referenceMap.Release();
            _mapper = null;
            _referenceMap = null;

            isDispose = true;
        }

        public TDomainEntity MapToDO(TPersistentObject persistentObject)
        {
            TDomainEntity result;

            if (_referenceMap.TryGetDomainEntity(persistentObject, out var relatedDomainEntity))
            {
                //If can find the associated result, change it based on it.
                result = _mapper.ToDomainEntity(persistentObject, (TDomainEntity)relatedDomainEntity);
            }
            else
            {
                var domainEntity = _mapper.ToDomainEntity<TDomainEntity, TPersistentObject>(persistentObject);
                _referenceMap.Add(domainEntity, persistentObject);

                result = domainEntity;
            }

            return result;
        }

        public IEnumerable<TDomainEntity> MapToDO(IEnumerable<TPersistentObject> persistentObject)
        {
            foreach (var po in persistentObject)
            {
                yield return MapToDO(po);
            }
        }

        public TPersistentObject MapToPO(TDomainEntity domainEntity)
        {
            TPersistentObject result;

            if (_referenceMap.TryGetPersistentObj(domainEntity, out var relatedPersistentObj))
            {
                //If can find the associated result, change it based on it.
                result = _mapper.ToPersistentObject(domainEntity, (TPersistentObject)relatedPersistentObj);
            }
            else
            {
                var persistentObjInstance = _mapper.ToPersistentObject<TDomainEntity, TPersistentObject>(domainEntity);
                _referenceMap.Add(domainEntity, persistentObjInstance);

                result = persistentObjInstance;
            }

            return result;
        }

        public IEnumerable<TPersistentObject> MapToPO(IEnumerable<TDomainEntity> domainEntity)
        {
            foreach (var entity in domainEntity)
            {
                yield return MapToPO(entity);
            }
        }
    }
}
