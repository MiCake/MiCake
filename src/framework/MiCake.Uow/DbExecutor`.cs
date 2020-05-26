using MiCake.Core.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// A base <see cref="IDbExecutor"/>.
    /// <see cref="TDbOject"/> is your Db Connect object.For example,DbContext in EFCore.
    /// </summary>
    /// <typeparam name="TDbOject">Db Connect object</typeparam>
    public abstract class DbExecutor<TDbOject> : IDbExecutor
         where TDbOject : class
    {
        public bool HasTransaction
        {
            get => CurrentTransaction != null;
        }

        /// <summary>
        /// The instance of <typeparamref name="TDbOject"/>.
        /// </summary>
        public TDbOject DbOjectInstance { get; protected set; }

        /// <summary>
        /// Current <see cref="ITransactionObject"/>.
        /// </summary>
        protected ITransactionObject CurrentTransaction { get; set; }

        public virtual void Dispose()
        {
            //The current object may be released outside.use catch to do nothing.
            try
            {
                if (DbOjectInstance != null)
                    (DbOjectInstance as IDisposable)?.Dispose();
            }
            catch
            {
            }
        }

        public void UseTransaction(ITransactionObject transactionObject)
        {
            if (HasTransaction)
                return;

            CheckValue.NotNull(transactionObject, nameof(transactionObject));

            if (SetTransaction(transactionObject))
                CurrentTransaction = transactionObject;
        }

        public async Task UseTransactionAsync(ITransactionObject transactionObject, CancellationToken cancellationToken = default)
        {
            if (HasTransaction)
                return;

            CheckValue.NotNull(transactionObject, nameof(transactionObject));

            if (await SetTransactionAsync(transactionObject, cancellationToken))
                CurrentTransaction = transactionObject;
        }

        /// <summary>
        /// Give transaction to current <see cref="DbOjectInstance"/>
        /// </summary>
        /// <param name="transaction"><see cref="ITransactionObject"/></param>
        protected abstract bool SetTransaction(ITransactionObject transaction);

        /// <summary>
        /// Give transaction to current <see cref="DbOjectInstance"/>
        /// </summary>
        /// <param name="transaction"><see cref="ITransactionObject"/></param>
        protected abstract Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken);
    }
}
