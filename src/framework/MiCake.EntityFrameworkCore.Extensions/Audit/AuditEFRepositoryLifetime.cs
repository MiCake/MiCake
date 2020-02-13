using MiCake.Audit;
using MiCake.DDD.Extensions;
using MiCake.EntityFrameworkCore.LifeTime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Extensions.Audit
{
    internal class AuditEFRepositoryLifetime : IEfRepositoryPreSaveChanges
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
       
        public Task PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default)
        {
            PreSaveChanges(entityState, entity);
            return Task.CompletedTask;
        }
    }
}
