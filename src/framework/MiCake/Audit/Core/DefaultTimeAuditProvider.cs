using MiCake.DDD.Extensions;
using System;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Provides creation and modification time for entities
    /// </summary>
    internal class DefaultTimeAuditProvider : IAuditProvider
    {
        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            switch (auditObjectModel.EntityState)
            {
                case RepositoryEntityState.Added:
                    SetCreationTime(auditObjectModel.AuditEntity);
                    break;
                    
                case RepositoryEntityState.Modified:
                    SetModificationTime(auditObjectModel.AuditEntity);
                    break;
            }
        }

        private static void SetCreationTime(object entity)
        {
            if (entity is IHasCreationTime hasCreationTime && 
                hasCreationTime.CreationTime == default)
            {
                hasCreationTime.CreationTime = DateTime.Now;
            }
        }

        private static void SetModificationTime(object entity)
        {
            if (entity is IHasModificationTime hasModificationTime)
            {
                hasModificationTime.ModificationTime = DateTime.Now;
            }
        }
    }
}
