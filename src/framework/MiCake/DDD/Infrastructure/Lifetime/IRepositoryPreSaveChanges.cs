using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Infrastructure.Lifetime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryPreSaveChanges : IRepositoryLifetime
    {
        /// <summary>
        /// Operations before domain object persistence
        /// </summary>
        ValueTask<RepositoryEntityStates> PreSaveChangesAsync(RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default);
    }
}
