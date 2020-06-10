using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Store;
using System.Collections.Generic;

namespace MiCake.Audit.Core
{
    public class DefaultAuditExecutor : IAuditExecutor
    {
        private IEnumerable<IAuditProvider> _providers;

        public DefaultAuditExecutor(IEnumerable<IAuditProvider> providers)
        {
            _providers = providers;
        }

        public virtual void Execute(object needAuditEntity, RepositoryEntityState entityState)
        {
            //Only deal with micake domain object.
            var entityType = needAuditEntity.GetType();
            if (!typeof(IPersistentObject).IsAssignableFrom(entityType) && !DomainTypeHelper.IsDomainObject(entityType))
                return;

            var model = new AuditObjectModel(needAuditEntity, entityState);

            foreach (var auditProvider in _providers)
            {
                auditProvider.ApplyAudit(model);
            }
        }
    }
}
