using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// Mark a api has transcation funcation.
    /// </summary>
    public interface ITransactionFeature : IDisposable
    {
        public bool IsCommit { get; }

        public bool IsRollback { get; }

        /// <summary>
        /// Commits all changes made to the database in the current transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Commits all changes made to the database in the current transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discards all changes made to the database in the current transaction.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Discards all changes made to the database in the current transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
