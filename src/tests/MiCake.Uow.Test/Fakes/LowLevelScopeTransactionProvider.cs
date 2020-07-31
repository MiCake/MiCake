using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    public class LowLevelScopeTransactionProvider : ITransactionProvider
    {
        //优先级低于TestScopeTransactionObject.
        //则当于TestScopeTransactionObject 共同存在的时候，该provider永远不会创建出事务
        public int Order => 0;

        public bool CanCreate(IDbExecutor dbExecutor)
        {
            return dbExecutor is ScopeDbExecutor;
        }

        public ITransactionObject GetTransactionObject(CreateTransactionContext context)
        {
            return new TestScopeTransactionObject(new System.Transactions.TransactionScope());
        }

        public Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            ITransactionObject result = new TestScopeTransactionObject(new System.Transactions.TransactionScope());
            return Task.FromResult(result);
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            var hasScopeTransaction = existedTrasactions.Where(s => !s.IsCommit && s is TestScopeTransactionObject).FirstOrDefault();

            return hasScopeTransaction;
        }
    }
}
