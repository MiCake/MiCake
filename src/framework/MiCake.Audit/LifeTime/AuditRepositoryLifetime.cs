using MiCake.Audit.Core;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.LifeTime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Audit.LifeTime
{
    internal class AuditRepositoryLifetime : IRepositoryPreSaveChanges
    {
        private IAuditExecutor _auditExecutor;

        public AuditRepositoryLifetime(IAuditExecutor auditExecutor)
        {
            _auditExecutor = auditExecutor;
        }

        public void PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            _auditExecutor.Execute(entity, entityState);
        }

        public Task PreSaveChangesAsync(RepositoryEntityState entityState,
                                        object entity,
                                        CancellationToken cancellationToken = default)
        {
            _auditExecutor.Execute(entity, entityState);

            return Task.CompletedTask;
        }
    }
}
