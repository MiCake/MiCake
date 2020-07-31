using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    //优先级中等，只接受类型为DemoDbExecutor的操作对象。
    //永远不会复用事务。
    public class TestDemoTransactionProvider : ITransactionProvider
    {
        public int Order => 0;

        public bool CanCreate(IDbExecutor dbExecutor)
        {
            return dbExecutor is DemoDbExecutor;
        }

        public ITransactionObject GetTransactionObject(CreateTransactionContext context)
        {
            return new TestDemoTransactionObject(2020);
        }

        public Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((ITransactionObject)new TestDemoTransactionObject(2020));
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            return null;
        }
    }
}
