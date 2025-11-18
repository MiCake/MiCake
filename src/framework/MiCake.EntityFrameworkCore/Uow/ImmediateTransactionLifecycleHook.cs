using MiCake.DDD.Uow;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Lifecycle hook for Unit of Work that handles immediate transaction initialization.
    /// When a UoW is created with TransactionInitializationMode.Immediate,
    /// this hook will immediately initialize transactions for all registered DbContext types.
    /// </summary>
    public class ImmediateTransactionLifecycleHook : IUnitOfWorkLifecycleHook
    {
        private readonly IImmediateTransactionInitializer _initializer;
        private readonly ILogger<ImmediateTransactionLifecycleHook> _logger;

        /// <summary>
        /// This hook applies only to Immediate initialization mode
        /// </summary>
        public TransactionInitializationMode? ApplicableMode => TransactionInitializationMode.Immediate;

        public ImmediateTransactionLifecycleHook(
            IImmediateTransactionInitializer initializer,
            ILogger<ImmediateTransactionLifecycleHook> logger)
        {
            _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnUnitOfWorkCreatedAsync(IUnitOfWork unitOfWork, UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Immediate transaction initialization requested for UoW {UowId}", unitOfWork.Id);

            // Call the initializer to set up all registered DbContext types
            await _initializer.InitializeTransactionsAsync(unitOfWork, cancellationToken).ConfigureAwait(false);
        }
    }
}
