using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Lifetime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Audit.Lifetime
{
    internal class SoftDeletionRepositoryLifetime : IRepositoryPreSaveChanges
    {
        /// <summary>
        /// Because it needs to change the state of the entity, it should be placed at the end of execution
        /// </summary>
        public int Order { get; set; } = 1000;

        public RepositoryEntityState PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            if (entity is ISoftDeletion softDeletion)
            {
                if (entityState == RepositoryEntityState.Deleted)
                {
                    softDeletion.IsDeleted = true;
                    entityState = RepositoryEntityState.Modified;
                }
                else
                {
                    softDeletion.IsDeleted = false;
                }
            }
            return entityState;
        }

        public ValueTask<RepositoryEntityState> PreSaveChangesAsync(RepositoryEntityState entityState,
                                                                    object entity,
                                                                    CancellationToken cancellationToken = default)
        {
            entityState = PreSaveChanges(entityState, entity);
            return new ValueTask<RepositoryEntityState>(entityState);
        }
    }
}
