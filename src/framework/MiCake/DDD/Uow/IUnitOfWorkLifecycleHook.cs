using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Hook interface for customizing Unit of Work lifecycle.
    /// Implementations can be registered in DI to perform additional initialization or cleanup.
    /// </summary>
    public interface IUnitOfWorkLifecycleHook
    {
        /// <summary>
        /// Called after a Unit of Work is created but before it's returned to the caller.
        /// Can be used for immediate transaction initialization or other setup tasks.
        /// </summary>
        /// <param name="unitOfWork">The newly created unit of work</param>
        /// <param name="options">The options used to create the unit of work</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OnUnitOfWorkCreatedAsync(IUnitOfWork unitOfWork, UnitOfWorkOptions options, CancellationToken cancellationToken = default);
    }
}
