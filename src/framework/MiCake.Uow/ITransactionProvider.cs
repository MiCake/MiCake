using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// A provider for <see cref="ITransactionObject"/>.
    /// </summary>
    public interface ITransactionProvider
    {
        /// Gets the order value for determining the order of execution of providers.
        /// Providers execute in ascending numeric value of the <see cref="Order"/> property.
        public int Order { get; }

        /// <summary>
        /// Determine whether the current <see cref="IDbExecutor"/> accepts the transaction created by the <see cref="ITransactionProvider"/>
        /// </summary>
        /// <param name="dbExecutor"><see cref="IDbExecutor"/></param>
        /// <returns>If true,Indicate current DbExecutor accepted this provider</returns>
        bool CanCreate(IDbExecutor dbExecutor);

        /// <summary>
        /// Reuse existing transactions.
        /// </summary>
        /// <param name="existedTrasactions">existing <see cref="ITransactionObject"/></param>
        /// <param name="dbExecutor">current <see cref="IDbExecutor"/></param>
        /// <returns>Return if there is a suitable transaction, otherwise return null.</returns>
        ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor);

        /// <summary>
        /// Get a <see cref="ITransactionObject"/>
        /// </summary>
        /// <param name="context"><see cref="CreateTransactionContext"/></param>
        ITransactionObject GetTransactionObject(CreateTransactionContext context);

        /// <summary>
        /// Get a <see cref="ITransactionObject"/> asynchronous.
        /// </summary>
        /// <param name="context"><see cref="CreateTransactionContext"/></param>
        /// <param name="cancellationToken"></param>
        Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default);
    }
}
