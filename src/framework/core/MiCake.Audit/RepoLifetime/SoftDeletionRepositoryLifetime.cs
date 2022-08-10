using MiCake.Audit.SoftDeletion;
using MiCake.Cord;
using MiCake.Cord.Lifetime;

namespace MiCake.Audit.RepoLifetime
{
    internal class SoftDeletionRepositoryLifetime : IRepositoryPreSaveChanges
    {
        /// <summary>
        /// Because it needs to change the state of the entity, it should be placed at the end of execution
        /// </summary>
        public int Order { get; set; } = 1000;

        public RepositoryEntityState PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            if (entity is ISoftDeletion)
            {
                if (entityState == RepositoryEntityState.Deleted)
                {
                    // change entity state to modified to avoid delete.
                    entityState = RepositoryEntityState.Modified;
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
