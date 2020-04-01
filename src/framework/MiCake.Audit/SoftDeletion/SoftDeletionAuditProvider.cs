using MiCake.Audit.Core;
using MiCake.DDD.Extensions;
using System;

namespace MiCake.Audit.SoftDeletion
{
    internal class SoftDeletionAuditProvider : IAuditProvider
    {
        public void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel.EntityState != RepositoryEntityState.Deleted)
                return;

            var entity = auditObjectModel.AuditEntity;
            if (!(entity is ISoftDeletion softDeletionObj))
                return;

            softDeletionObj.IsDeleted = true;

            if (entity is IHasDeletionTime hasDeletionTimeObj)
                hasDeletionTimeObj.DeletionTime = DateTime.Now;
        }
    }
}
