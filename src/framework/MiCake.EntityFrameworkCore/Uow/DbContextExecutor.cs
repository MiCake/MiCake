using MiCake.Core.DependencyInjection;
using MiCake.Core.Util;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class DbContextExecutor<TDbContext> : DbExecutor<TDbContext>, IEFCoreDbExecutor
          where TDbContext : DbContext
    {
        private readonly MiCakeEFCoreOptions _options;

        public DbContextExecutor(TDbContext dbContext, IObjectAccessor<MiCakeEFCoreOptions> options) : base(dbContext)
        {
            if (dbContext.Database.CurrentTransaction != null)
            {
                //If there is a transaction to prove that the user is already using the dbcontext, the dbcontext is not hosted by the unit of work
                CurrentTransaction = new EFCoreTransactionObject(dbContext.Database.CurrentTransaction, dbContext, true);
            }

            _options = options.Value ?? throw new ArgumentNullException($"{nameof(MiCakeEFCoreOptions)} can not be null.");
        }

        DbContext IEFCoreDbExecutor.EFCoreDbContext => DbOjectInstance;

        public bool ShouldCommitTrxForEFCore { get => _options?.WillOpenTransactionForUow ?? false; }

        protected override async Task<bool> SetTransactionAsync(ITransactionObject transaction, CancellationToken cancellationToken)
        {
            CheckValue.NotNull(transaction, nameof(transaction));

            // if doestn't need to commit, just return true.
            if (transaction is EFCoreTransactionObject efTrasactionObj)
            {
                if (efTrasactionObj.NeedEFCoreCommit is false)
                {
                    return true;
                }
            }

            bool result = false;
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
            if (obj is DbContextExecutor<TDbContext> dbContextExecutor)
                return true;

            return ReferenceEquals(DbOjectInstance, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
