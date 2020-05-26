using MiCake.Core.Util;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionObject : ITransactionObject
    {
        public Guid ID { get; }
        public bool IsCommit { get; private set; }
        public Type TransactionType { get; private set; }
        public object TransactionInstance => _efCoreTransaction;

        private readonly IDbContextTransaction _efCoreTransaction;

        public EFCoreTransactionObject(IDbContextTransaction dbContextTransaction)
        {
            CheckValue.NotNull(dbContextTransaction, nameof(dbContextTransaction));

            ID = Guid.NewGuid();
            TransactionType = dbContextTransaction.GetType();

            _efCoreTransaction = dbContextTransaction;
        }

        public void Commit()
        {
            if (IsCommit)
                throw new InvalidOperationException($"The current transaction has been committed!");

            IsCommit = true;
            _efCoreTransaction.Commit();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsCommit)
                throw new InvalidOperationException($"The current transaction has been committed!");

            IsCommit = true;
            await _efCoreTransaction.CommitAsync(cancellationToken);
        }

        public void Dispose()
        {
            _efCoreTransaction.Dispose();
        }

        public void Rollback()
        {
            _efCoreTransaction.Rollback();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _efCoreTransaction.RollbackAsync(cancellationToken);
        }
    }
}
