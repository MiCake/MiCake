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

            if (context.EntityState != RepositoryEntityState.Deleted)
                return;

            var entity = context.Entity;
            if (entity is not ISoftDeletion softDeletionObj)
                return;

            softDeletionObj.IsDeleted = true;

            if (entity is IHasDeletionTime hasDeletionTimeObj)
                hasDeletionTimeObj.DeletionTime = DateTime.Now;
        }
    }
}
