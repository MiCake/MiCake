using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.LifeTime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryPreSaveChanges:IRepositoryLifetime
    {
        /// <summary>
        /// Operations before domain object persistence
        /// </summary>
        void PreSaveChanges(RepositoryEntityState entityState, object entity);

        /// <summary>
        /// Operations before domain object persistence
        /// </summary>
        Task PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default);

    }
}
