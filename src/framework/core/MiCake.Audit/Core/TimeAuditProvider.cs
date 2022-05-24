using MiCake.Cord;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Give entity creation time or modifaction time.
    /// </summary>
    internal class DefaultTimeAuditProvider : IAuditProvider
    {
        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel.EntityState == RepositoryEntityState.Modified)
            {
                SetModifactionTime(auditObjectModel.AuditEntity);
            }
        }

        private void SetModifactionTime(object entity)
        {
            if (entity is not IHasModificationTime hasModificationTimeObj)
                return;

            hasModificationTimeObj.ModificationTime = DateTime.UtcNow;
        }
    }
}
