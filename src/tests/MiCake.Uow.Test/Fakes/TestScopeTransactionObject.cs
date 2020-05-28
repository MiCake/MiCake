﻿using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace MiCake.Uow.Test.Fakes
{
    public class TestScopeTransactionObject : BaseTransactionObject<TransactionScope>
    {
        public string Source { get; set; }

        public TestScopeTransactionObject(TransactionScope transactionSource,string source ="None") : base(transactionSource)
        {
            Source = source;
        }

        public override void Commit()
        {
            this.IsCommit = true;
            Transaction.Complete();
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            this.IsCommit = true;
            Transaction.Complete();

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Transaction.Dispose();
        }

        public override void Rollback()
        {
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
