using MiCake.DDD.Extensions.Store;
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
            _mapStore = _mapStore ?? new Dictionary<object, object>();
            _mapStore[domainEntity] = persistentObj;
        }

        public bool TryGet(object domainEntity, out object persistentObj)
        {
            return _mapStore.TryGetValue(domainEntity, out persistentObj);
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


        class PoMapKey
        {
            public PoMapKey()
            {
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
