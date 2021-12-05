using MiCake.Audit.Core;
using MiCake.DDD.Connector;
using MiCake.DDD.Connector.Lifetime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Audit.Lifetime
{
    internal class AuditRepositoryLifetime : IRepositoryPreSaveChanges
    {
        private readonly IAuditExecutor _auditExecutor;

        public AuditRepositoryLifetime(IAuditExecutor auditExecutor)
        {
            _auditExecutor = auditExecutor;
        }

        public int Order { get; set; } = -1000;

        public RepositoryEntityState PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            _auditExecutor.Execute(entity, entityState);
            return entityState;
        }

        public ValueTask<RepositoryEntityState> PreSaveChangesAsync(RepositoryEntityState entityState,
                                        object entity,
                                        CancellationToken cancellationToken = default)
        {
            _auditExecutor.Execute(entity, entityState);

            return new ValueTask<RepositoryEntityState>(entityState);
        }
    }
}
