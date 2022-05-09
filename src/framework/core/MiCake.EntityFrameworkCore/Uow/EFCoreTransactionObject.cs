using MiCake.Uow;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionObject : ITransactionObject
    {
        public Guid ID { get; }
        public bool IsCommit { get; private set; }
        public Type TransactionType { get; private set; }
        public object TransactionInstance => _dbContext;

        private DbContext _dbContext;

        public EFCoreTransactionObject(DbContext dbContext, Type dbContextType)
        {
            ID = Guid.NewGuid();
            TransactionType = dbContextType;
            _dbContext = dbContext!;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            //Only called once.
            if (IsCommit)
                return;

            IsCommit = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
