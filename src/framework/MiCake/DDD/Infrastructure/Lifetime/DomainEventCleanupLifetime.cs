using MiCake.DDD.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Infrastructure.Lifetime
{
    /// <summary>
    /// Clears domain events from entities after successful save to prevent re-dispatch.
    /// </summary>
    internal class DomainEventCleanupLifetime : IRepositoryPostSaveChanges
    {
        public int Order { get; set; } = 1000; // Run after other post-save handlers

        public ValueTask<RepositoryEntityStates> PostSaveChangesAsync(RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
        {
            if (entity is IEntity domainEntity)
            {
                domainEntity.ClearDomainEvents();
            }

            return new ValueTask<RepositoryEntityStates>(entityState);
        }
    }
}
