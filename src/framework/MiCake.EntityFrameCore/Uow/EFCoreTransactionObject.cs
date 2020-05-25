using MiCake.Uow;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionObject : ITransactionObject
    {
        public Guid ID => throw new NotImplementedException();

        public bool IsCommit => throw new NotImplementedException();

        public Type TransactionType => throw new NotImplementedException();

        public object TransactionInstance => throw new NotImplementedException();

        public EFCoreTransactionObject()
        {
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
