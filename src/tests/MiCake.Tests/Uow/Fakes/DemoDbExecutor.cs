using System;
using System.Threading;
using System.Threading.Tasks;
using MiCake.DDD.Uow;

namespace MiCake.Uow.Tests.Fakes
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

        protected override Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken)
        {
            var result = false;
            if (transaction.TransactionInstance is int)
            {
                TransactionObject = transaction;
                DbOjectInstance.SetTransaction(transaction.TransactionInstance);
                result = true;
            }

            return Task.FromResult(result);
        }

        public override void Dispose()
        {
            IsDispose = true;

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
