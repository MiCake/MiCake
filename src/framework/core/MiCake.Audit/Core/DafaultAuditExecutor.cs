using MiCake.Cord;
using MiCake.DDD.Domain.Helper;

namespace MiCake.Audit.Core
{
    internal class DefaultAuditExecutor : IAuditExecutor
    {
        private readonly IEnumerable<IAuditProvider> _providers;

        public DefaultAuditExecutor(IEnumerable<IAuditProvider> providers)
        {
            _providers = providers;
        }

        public virtual void Execute(object needAuditEntity, RepositoryEntityState entityState)
        {
            //Only deal with micake domain object.
            var entityType = needAuditEntity.GetType();
            if (!DomainTypeHelper.IsDomainObject(entityType))
                return;

            var model = new AuditObjectModel(needAuditEntity, entityState);

            foreach (var auditProvider in _providers)
            {
                auditProvider.ApplyAudit(model);
            }
        }
    }
}
