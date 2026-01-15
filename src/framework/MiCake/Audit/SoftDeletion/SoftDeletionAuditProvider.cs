using MiCake.Audit.Core;
using MiCake.DDD.Infrastructure;
using System;

namespace MiCake.Audit.SoftDeletion
{
    internal class SoftDeletionAuditProvider : IAuditProvider
    {
        public virtual void ApplyAudit(AuditOperationContext context)
        {
            if (context?.Entity == null)
                return;

            if (context.EntityState != RepositoryEntityStates.Deleted)
                return;

            var entity = context.Entity;
            if (entity is not ISoftDeletable softDeletionObj)
                return;

            softDeletionObj.IsDeleted = true;

            if (entity is IHasDeletedAt hasDeletionTimeObj)
                hasDeletionTimeObj.DeletedAt = DefaultTimeAuditProvider.CurrentTimeProvider?.Invoke() ?? DateTime.UtcNow;
        }
    }
}
