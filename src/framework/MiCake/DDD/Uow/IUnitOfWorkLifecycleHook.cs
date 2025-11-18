using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow;

/// <summary>
/// Hook interface for customizing Unit of Work lifecycle.
/// Implementations can be registered in DI to perform additional initialization or cleanup.
/// </summary>
public interface IUnitOfWorkLifecycleHook
{
    /// <summary>
    /// Gets the initialization mode(s) this hook applies to.
    /// If null, the hook applies to all modes.
    /// </summary>
    TransactionInitializationMode? ApplicableMode { get; }

    /// <summary>
    /// Called after a Unit of Work is created.
    /// For Immediate mode: Called immediately after creation, before returning to caller.
    /// For Lazy mode: Called before first transaction activation (before ActivatePendingResourcesAsync).
    /// </summary>
    /// <param name="unitOfWork">The newly created unit of work</param>
    /// <param name="options">The options used to create the unit of work</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnUnitOfWorkCreatedAsync(IUnitOfWork unitOfWork, UnitOfWorkOptions options, CancellationToken cancellationToken = default);
}
