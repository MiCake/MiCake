using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
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

        public static RepositoryEntityStates PreSaveChanges(RepositoryEntityStates entityState, object entity)
        {
            if (entity is ISoftDeletable softDeletion)
            {
                if (entityState == RepositoryEntityStates.Deleted)
                {
                    softDeletion.IsDeleted = true;
                    entityState = RepositoryEntityStates.Modified;
                }
                else
                {
                    softDeletion.IsDeleted = false;
                }
            }
            return entityState;
        }

        public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(RepositoryEntityStates entityState,
                                                                    object entity,
                                                                    CancellationToken cancellationToken = default)
        {
            entityState = PreSaveChanges(entityState, entity);
            return new ValueTask<RepositoryEntityStates>(entityState);
        }
    }
}
