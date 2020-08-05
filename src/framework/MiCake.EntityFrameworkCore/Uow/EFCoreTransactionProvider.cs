using MiCake.Core.Util;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionProvider : ITransactionProvider
    {
        /// <summary>
        /// EF transaction has a low  priority.
        /// When there are transactions of trasactionscope or ADO.NET in the outside world, ensure that they can be shared
        /// </summary>
        public int Order => 100;

        public bool CanCreate(IDbExecutor dbExecutor)
            => dbExecutor is IEFCoreDbExecutor;

        public ITransactionObject GetTransactionObject(CreateTransactionContext context)
        {
            var dbContext = CheckContextAndGetDbContext(context);

            var dbContextTransaction = dbContext.Database.BeginTransaction();
            return new EFCoreTransactionObject(dbContextTransaction, dbContext);
        }

        public async Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            var dbContext = CheckContextAndGetDbContext(context);

            var dbContextTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            return new EFCoreTransactionObject(dbContextTransaction, dbContext);
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            if (existedTrasactions.Count() == 0)
                return null;

            ITransactionObject optimalTransaction = null;

            IEFCoreDbExecutor eFCoreDbExecutor = dbExecutor as IEFCoreDbExecutor;
            if (eFCoreDbExecutor == null)
                return null;

            //DbContext only can receive DbTransaction.
            //Todo:If there has more DbTransaction,How to compare?
            optimalTransaction = existedTrasactions.FirstOrDefault(s =>
            {
                if (!s.IsCommit && s.TransactionInstance is DbTransaction dbTransaction)
                {
                    var dbContext = eFCoreDbExecutor.EFCoreDbContext;
                    //Get EF Core DbConnection.
                    //Only detail relational transaction.
                    var relationDatabase = (dbContext.Database as IDatabaseFacadeDependenciesAccessor)?.Dependencies as IRelationalDatabaseFacadeDependencies;

                    if (relationDatabase == null)
                        return false;

                    //It's mean they use same database.

#pragma warning disable EF1001 // Internal EF Core API usage.
                    return relationDatabase.RelationalConnection.DbConnection.ConnectionString.Equals(dbTransaction.Connection.ConnectionString);
#pragma warning restore EF1001 // Internal EF Core API usage.
                }
                return false;
            });

            return optimalTransaction;
        }

        private DbContext CheckContextAndGetDbContext(CreateTransactionContext context)
        {
            CheckValue.NotNull(context.CurrentUnitOfWork, nameof(context.CurrentUnitOfWork));
            CheckValue.NotNull(context.CurrentDbExecutor, nameof(context.CurrentDbExecutor));

            var efDbExecutor = context.CurrentDbExecutor as IEFCoreDbExecutor ??
                                throw new ArgumentException($"Current DbExecutor can not assignable from {nameof(IEFCoreDbExecutor)}");

            CheckValue.NotNull(efDbExecutor.EFCoreDbContext, nameof(efDbExecutor.EFCoreDbContext));

            return efDbExecutor.EFCoreDbContext;
        }
    }
}
