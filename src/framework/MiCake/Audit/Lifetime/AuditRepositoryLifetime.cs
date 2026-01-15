using MiCake.Audit.Core;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
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

        public RepositoryEntityStates PreSaveChanges(RepositoryEntityStates entityState, object entity)
        {
            _auditExecutor.Execute(entity, entityState);
            return entityState;
        }

        public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(RepositoryEntityStates entityState,
                                        object entity,
                                        CancellationToken cancellationToken = default)
        {
            _auditExecutor.Execute(entity, entityState);

            return new ValueTask<RepositoryEntityStates>(entityState);
        }
    }
}
