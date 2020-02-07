using MiCake.Audit;
using MiCake.DDD.Extensions;

namespace MiCake.EntityFrameworkCore.Extensions.Audit
{
    public class AuditEFRepositoryLifetime : IEfRepositoryLifetime
    {
        private readonly IAuditContext _auditContext;

        public AuditEFRepositoryLifetime(IAuditContext auditContext)
        {
            _auditContext = auditContext;
        }

        public void PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            if (entityState == RepositoryEntityState.Unchanged)
                return;

            if (entityState == RepositoryEntityState.Added)
                _auditContext.ObjectSetter.SetCreationInfo(entity);

            if (entityState == RepositoryEntityState.Modified)
                _auditContext.ObjectSetter.SetModificationInfo(entity);

            if (entityState == RepositoryEntityState.Deleted)
                _auditContext.ObjectSetter.SetDeletionInfo(entity);
        }

        public void PostSaveChanges(RepositoryEntityState entityState, object entity)
        {
        }
    }
}
