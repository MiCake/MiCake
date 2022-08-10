using MiCake.Cord;
using MiCake.Core.Util.Reflection;

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
            if (entity is not IHasModificationTime)
                return;

            ReflectionHelper.SetValueByPath(entity, typeof(IHasModificationTime), nameof(IHasModificationTime.ModificationTime), DateTime.UtcNow);
        }
    }
}
