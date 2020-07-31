using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    public class TestDemoTransactionObject : BaseTransactionObject<int>
    {
        public TestDemoTransactionObject(int transactionSource) : base(transactionSource)
        {
        }

        public override void Commit()
        {
            IsCommit = true;
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IsCommit = true;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {

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
