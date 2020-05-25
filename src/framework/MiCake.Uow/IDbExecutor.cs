using System;

namespace MiCake.Uow
{
    /// <summary>
    /// Execute database operation object.
    /// It will maintain a reference to the database operand.For example:DbContext in EFCore.
    /// </summary>
    public interface IDbExecutor : IDisposable
    {
        /// <summary>
        /// Whether this <see cref="IDbExecutor"/> has a transaction.
        /// </summary>
        bool HasTransaction { get; }

        /// <summary>
        /// Set transaction for current executor.
        /// </summary>
        /// <param name="transactionObject"><see cref="ITransactionObject"/></param>
        void UseTransaction(ITransactionObject transactionObject);
    }
}
