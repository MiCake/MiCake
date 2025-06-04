using MiCake.Core.Util;
using MiCake.DDD.Uow;
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
        public Type? TransactionType { get; private set; }
        public object? TransactionInstance => _efCoreTransaction;

        public bool NeedEFCoreCommit { get; set; } = false;

        private readonly IDbContextTransaction? _efCoreTransaction;
        private readonly DbContext _dbContext;

        public EFCoreTransactionObject(IDbContextTransaction? dbContextTransaction, DbContext dbContext, bool willCommit)
        {
            if (willCommit)
            {
                CheckValue.NotNull(dbContextTransaction, nameof(dbContextTransaction));

                TransactionType = dbContextTransaction.GetType();
                _efCoreTransaction = dbContextTransaction;
            }

            ID = Guid.NewGuid();
            NeedEFCoreCommit = willCommit;

            _dbContext = dbContext;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            //Only called once.
            if (IsCommit)
                return;

            IsCommit = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (NeedEFCoreCommit)
            {
                await _efCoreTransaction?.CommitAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
            _efCoreTransaction?.Dispose();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _efCoreTransaction?.RollbackAsync(cancellationToken);
        }
    }
}
