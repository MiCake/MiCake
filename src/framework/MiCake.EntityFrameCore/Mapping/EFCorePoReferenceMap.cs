using MiCake.DDD.Extensions.Store;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Mapping
{
    public class EFCorePoReferenceMap : IPersistentObjectReferenceMap
    {
        private Dictionary<object, object> _mapStore;

        public EFCorePoReferenceMap()
        {
        }

        public void AddOrUpdate(object domainEntity, object persistentObj)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(object domainEntity, out object persistentObj)
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            _mapStore?.Clear();
            _mapStore = null;
        }

        public Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            Release();
            return Task.CompletedTask;
        }
    }
}
