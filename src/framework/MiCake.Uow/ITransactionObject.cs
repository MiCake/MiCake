using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// Defining a transaction object will be used in the <see cref="IUnitOfWork"/>
    /// </summary>
    public interface ITransactionObject : IDisposable
    {
        /// <summary>
        /// Whether transaction is opened.
        /// </summary>
        public bool IsOpened { get; }

        /// <summary>
        /// The type of transaction object.
        /// </summary>
        public Type TransactionType { get; }

        /// <summary>
        /// Current transaction instance.
        /// </summary>
        public object TransactionInstance { get; }

        /// <summary>
        /// Open current transaction.
        /// </summary>
        ITransactionObject Open();

        /// <summary>
        /// Commits all changes made to the database in the current transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Commits all changes made to the database in the current transaction asynchronously.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///  Discards all changes made to the database in the current transaction.
        /// </summary>
        void Rollback();

        /// <summary>
        ///  Discards all changes made to the database in the current transaction asynchronously.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
