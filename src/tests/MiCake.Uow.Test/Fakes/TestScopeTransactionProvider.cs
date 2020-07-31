using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    //优先级高，接受类型为ScopeDbExecutor的执行对象。
    //会复用类型为TestScopeTransactionObject的事务对象。
    public class TestScopeTransactionProvider : ITransactionProvider
    {
        public int Order => -1000;

        public bool CanCreate(IDbExecutor dbExecutor)
        {
            return dbExecutor is ScopeDbExecutor;
        }

        public ITransactionObject GetTransactionObject(CreateTransactionContext context)
        {
            return new TestScopeTransactionObject(new System.Transactions.TransactionScope(), "TestScope");
        }

        public Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            ITransactionObject result = new TestScopeTransactionObject(new System.Transactions.TransactionScope(), "TestScope");
            return Task.FromResult(result);
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            var hasScopeTransaction = existedTrasactions.Where(s => !s.IsCommit && s is TestScopeTransactionObject).FirstOrDefault();

            return hasScopeTransaction;
        }
    }
}
