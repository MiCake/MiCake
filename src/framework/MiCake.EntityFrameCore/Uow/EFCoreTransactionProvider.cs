using MiCake.Uow;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionProvider : ITransactionProvider
    {
        public int Order => 0;

        public bool CanCreate(IDbExecutor dbExecutor)
        {
            return false;
        }

        public ITransactionObject GetTransactionObject(CreateTransactionContext context)
        {
            throw new NotImplementedException();
        }

        public Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            throw new NotImplementedException();
        }
    }
}
