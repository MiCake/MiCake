using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.LifeTime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Audit.LifeTime
{
    internal class AuditRepositoryLifetime : IRepositoryPreSaveChanges
    {
        //public void PreSaveChanges(RepositoryEntityState entityState, object entity)
        //{
        //    if (entityState == RepositoryEntityState.Unchanged)
        //        return;

        //    if (entityState == RepositoryEntityState.Added)
        //        _auditContext.ObjectSetter.SetCreationInfo(entity);

        //    if (entityState == RepositoryEntityState.Modified)
        //        _auditContext.ObjectSetter.SetModificationInfo(entity);

        //    if (entityState == RepositoryEntityState.Deleted)
        //        _auditContext.ObjectSetter.SetDeletionInfo(entity);
        //}

        public void PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            throw new System.NotImplementedException();
        }

        public Task PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
