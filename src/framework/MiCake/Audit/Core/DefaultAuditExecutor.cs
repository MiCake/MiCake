using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Infrastructure;
using System.Collections.Generic;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Default implementation of <see cref="IAuditExecutor"/>.
    /// </summary>
    internal class DefaultAuditExecutor : IAuditExecutor
    {
        private readonly IEnumerable<IAuditProvider> _providers;

        public DefaultAuditExecutor(IEnumerable<IAuditProvider> providers)
        {
            _providers = providers;
        }

        public virtual void Execute(object needAuditEntity, RepositoryEntityStates entityState)
        {
            //Only deal with micake domain object.
            var entityType = needAuditEntity.GetType();
            if (!DomainTypeHelper.IsDomainObject(entityType))
                return;

            var model = new AuditOperationContext(needAuditEntity, entityState);

            foreach (var auditProvider in _providers)
            {
                auditProvider.ApplyAudit(model);
            }
        }
    }
}
