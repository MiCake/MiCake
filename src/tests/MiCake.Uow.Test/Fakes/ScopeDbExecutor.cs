using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    public class ScopeDbExecutor : DbExecutor<FakeDbContext>
    {
        public bool IsDispose { get; set; }

        public ITransactionObject TransactionObject { get; set; }

        public ScopeDbExecutor(FakeDbContext dbContext) : base(dbContext)
        {
        }

        protected override bool SetTransaction(ITransactionObject transaction)
        {
            TransactionObject = transaction;
            DbOjectInstance.SetTransaction(transaction.TransactionInstance);
            return true;
        }

        protected override Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken)
        {
            TransactionObject = transaction;
            DbOjectInstance.SetTransaction(transaction.TransactionInstance);
            return Task.FromResult(true);
        }

        public override void Dispose()
        {
            IsDispose = true;

            base.Dispose();
        }
    }

    public class FakeDbContext : IDisposable
    {
        public bool IsDispose { get; set; }

        public object Trsansaction { get; set; }

        public FakeDbContext()
        {
        }

        public void Dispose()
        {
            if (IsDispose)
                throw new Exception("Has disposed");

            IsDispose = true;
        }

        public void SetTransaction(object transaction)
            => Trsansaction = transaction;
    }
}
