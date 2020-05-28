using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Test.Fakes
{
    public class DemoDbExecutor : DbExecutor<DemoDbContext>
    {
        public bool IsDispose { get; set; }

        public ITransactionObject TransactionObject { get; set; }

        public DemoDbExecutor()
        {
        }

        public DemoDbExecutor(DemoDbContext instance) : base(instance)
        {
        }

        protected override bool SetTransaction(ITransactionObject transaction)
        {
            if (transaction.TransactionInstance is int)
            {
                TransactionObject = transaction;
                this.DbOjectInstance.SetTransaction(transaction.TransactionInstance);
                return true;
            }

            return false;
        }

        protected override Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken)
        {
            var result =  SetTransaction(transaction);

            return Task.FromResult(result);
        }

        public override void Dispose()
        {
            this.IsDispose = true;

            base.Dispose();
        }
    }

    public class DemoDbContext : IDisposable
    {
        public bool IsDispose { get; set; }

        public object Transaction { get; set; }

        public DemoDbContext()
        {
        }

        public void Dispose()
        {
            if (IsDispose)
                throw new Exception("Has disposed");

            IsDispose = true;
        }

        public void SetTransaction(object transaction)
            => Transaction = transaction;
    }
}
