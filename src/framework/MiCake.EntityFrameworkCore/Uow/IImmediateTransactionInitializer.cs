using MiCake.DDD.Uow;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Interface for immediately initializing transactions for registered DbContext types.
    /// This is used when UnitOfWork is configured with TransactionInitializationMode.Immediate.
    /// </summary>
    public interface IImmediateTransactionInitializer
    {
        /// <summary>
        /// Immediately initialize transactions for all registered DbContext types in the given UoW.
        /// This is called when a UoW is created with Immediate initialization mode.
        /// </summary>
        /// <param name="unitOfWork">The unit of work to initialize transactions for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task InitializeTransactionsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);
    }
}
