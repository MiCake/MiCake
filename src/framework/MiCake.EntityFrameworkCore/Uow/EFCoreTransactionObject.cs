using MiCake.Core.Util;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
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
        private readonly DbContext _dbContext;

        public EFCoreTransactionObject(IDbContextTransaction dbContextTransaction, DbContext dbContext)
        {
            CheckValue.NotNull(dbContextTransaction, nameof(dbContextTransaction));

            ID = Guid.NewGuid();
            TransactionType = dbContextTransaction.GetType();

            _efCoreTransaction = dbContextTransaction;
            _dbContext = dbContext;
        }

        public void Commit()
        {
            //Only called once.
            if (IsCommit)
                return;

            IsCommit = true;

            _dbContext.SaveChanges();
            _efCoreTransaction.Commit();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            //Only called once.
            if (IsCommit)
                return;

            IsCommit = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
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
