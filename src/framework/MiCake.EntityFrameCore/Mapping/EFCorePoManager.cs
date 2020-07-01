using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;

namespace MiCake.EntityFrameworkCore.Mapping
{
    public class EFCorePoManager<TDomainEntity, TPersistentObject>
        where TDomainEntity : IEntity
        where TPersistentObject : IPersistentObject
    {
        private readonly IPersistentObjectReferenceMap _referenceMap = new EFCorePoReferenceMap();

        public EFCorePoManager()
        {
        }

        public TDomainEntity MapToDO(TPersistentObject persistentObject)
        {
            return default;
        }

        public TPersistentObject MapToPO(TDomainEntity domainEntity)
        {
            return default;
        }
    }
}
