using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Internal contract used by framework components to register resources with and activate a unit of work.
    /// </summary>
    public interface IUnitOfWorkInternal
    {
        /// <summary>
        /// Registers a resource (like a DbContext wrapper) with this unit of work.
        /// Called only by framework components. Synchronous and lightweight; prepares the resource without I/O.
        /// Nested units of work delegate registration to the root.
        /// </summary>
        /// <param name="resource">The resource to register</param>
        void RegisterResource(IUnitOfWorkResource resource);

        /// <summary>
        /// Ensures every registered resource has an active transaction.
        /// Rejected on read-only units of work. Idempotent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the unit of work as rollback-only so a later commit is rejected.
        /// Called by framework components when a save operation can no longer proceed
        /// safely (for example, after a controlled re-entry failure). Idempotent.
        /// </summary>
        void MarkRollbackOnly();
    }
}
