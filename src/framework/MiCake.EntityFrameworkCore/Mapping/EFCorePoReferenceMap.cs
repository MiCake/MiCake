using MiCake.Core.Util.Collections;
using MiCake.DDD.Extensions.Store;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Mapping
{
    public class EFCorePoReferenceMap : IPersistentObjectReferenceMap
    {
        private Map<object, object> _mapStore = new Map<object, object>();

        public EFCorePoReferenceMap()
        {
        }

        public void Add(object domainEntity, object persistentObj)
        {
            _mapStore.Add(domainEntity, persistentObj);
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

        public bool TryGetPersistentObj(object domainEntity, out object persistentObj)
        {
            return _mapStore.Forward.TryGetValue(domainEntity, out persistentObj);
        }

        public bool TryGetDomainEntity(object persistentObj, out object domainEntity)
        {
            return _mapStore.Reverse.TryGetValue(persistentObj, out domainEntity);
        }
    }
}
