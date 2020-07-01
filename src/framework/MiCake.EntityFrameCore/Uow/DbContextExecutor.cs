using MiCake.Core.Util;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class DbContextExecutor<TDbContext> : DbExecutor<TDbContext>, IEFCoreDbExecutor
          where TDbContext : DbContext
    {
        public DbContextExecutor(TDbContext dbContext) : base(dbContext)
        {
            if (dbContext.Database.CurrentTransaction != null)
            {
                //If there is a transaction to prove that the user is already using the dbcontext, the dbcontext is not hosted by the unit of work
                CurrentTransaction = new EFCoreTransactionObject(dbContext.Database.CurrentTransaction, dbContext);
            }
        }

        DbContext IEFCoreDbExecutor.EFCoreDbContext => DbOjectInstance;

        protected override bool SetTransaction(ITransactionObject transaction)
        {
            CheckValue.NotNull(transaction, nameof(transaction));

            bool result = false; ;
            //Only receive DbTransaction.
            if (transaction.TransactionInstance is DbTransaction dbTransaction)
            {
                try
                {
                    DbOjectInstance.Database.UseTransaction(dbTransaction);
                    result = true;
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        protected override async Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken)
        {
            CheckValue.NotNull(transaction, nameof(transaction));

            bool result = false; ;
            //Only receive DbTransaction.
            if (transaction.TransactionInstance is DbTransaction dbTransaction)
            {
                try
                {
                    await DbOjectInstance.Database.UseTransactionAsync(dbTransaction, cancellationToken);
                    result = true;
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            //If dbcontext is the same, it is considered the same
            if (!(obj is TDbContext))
                return false;

            return ReferenceEquals(DbOjectInstance, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
