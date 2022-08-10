using MiCake.Audit.Core;
using MiCake.Cord;
using MiCake.Core.Util.Reflection;

namespace MiCake.Audit.SoftDeletion
{
    internal class SoftDeletionAuditProvider : IAuditProvider
    {
        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel.EntityState != RepositoryEntityState.Deleted)
                return;

            var entity = auditObjectModel.AuditEntity;
            if (entity is not ISoftDeletion)
                return;

            ReflectionHelper.SetValueByPath(entity, typeof(ISoftDeletion), nameof(ISoftDeletion.IsDeleted), true);

            if (entity is IHasDeletionTime)
            {
                ReflectionHelper.SetValueByPath(entity, typeof(IHasDeletionTime), nameof(IHasDeletionTime.DeletionTime), DateTime.Now);
            }
        }
    }
}
