using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Infrastructure.Lifetime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryPostSaveChanges : IRepositoryLifetime
    {
        /// <summary>
        /// Operations after domain object persistence
        /// </summary>
        ValueTask<RepositoryEntityStates> PostSaveChangesAsync(RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default);
    }
}
