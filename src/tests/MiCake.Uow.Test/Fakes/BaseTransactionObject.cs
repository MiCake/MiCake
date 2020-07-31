using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Tests.Fakes
{
    public abstract class BaseTransactionObject<TTransactionSource> : ITransactionObject
    {
        public Guid ID { get; set; }

        public bool IsCommit { get; set; }

        public Type TransactionType { get; set; }

        public object TransactionInstance { get; set; }

        public TTransactionSource Transaction => (TTransactionSource)TransactionInstance;

        public BaseTransactionObject(TTransactionSource transactionSource)
        {
            ID = Guid.NewGuid();
            TransactionType = transactionSource.GetType();
            TransactionInstance = transactionSource;
        }

        public abstract void Commit();

        public abstract Task CommitAsync(CancellationToken cancellationToken = default);

        public abstract void Dispose();

        public abstract void Rollback();

        public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(obj, TransactionInstance);
        }
    }
}
