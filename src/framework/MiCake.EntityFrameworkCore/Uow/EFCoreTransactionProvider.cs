using MiCake.Core.Util;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public async Task<ITransactionObject> GetTransactionObjectAsync(CreateTransactionContext context, CancellationToken cancellationToken = default)
        {
            var (dbcontext, willCommitTrx) = CheckContextAndGetDbContext(context);

            if (willCommitTrx)
            {
                var dbContextTransaction = await dbcontext.Database.BeginTransactionAsync(cancellationToken);
                return new EFCoreTransactionObject(dbContextTransaction, dbcontext, true);
            }
            else
            {
                return new EFCoreTransactionObject(null, dbcontext, false);
            }
        }

        public ITransactionObject Reused(IEnumerable<ITransactionObject> existedTrasactions, IDbExecutor dbExecutor)
        {
            if (!existedTrasactions.Any())
                return null;

            ITransactionObject optimalTransaction = null;

            if (dbExecutor is not IEFCoreDbExecutor eFCoreDbExecutor)
                return null;

            //DbContext only can receive DbTransaction.
            optimalTransaction = existedTrasactions.FirstOrDefault(s =>
            {
                /*
                 * todo:how to reuse same transation.
                 * .....
                 */

                return false;
            });

            return optimalTransaction;
        }

        private (DbContext dbcontext, bool willCommitTrx) CheckContextAndGetDbContext(CreateTransactionContext context)
        {
            CheckValue.NotNull(context.CurrentUnitOfWork, nameof(context.CurrentUnitOfWork));
            CheckValue.NotNull(context.CurrentDbExecutor, nameof(context.CurrentDbExecutor));

            var efDbExecutor = context.CurrentDbExecutor as IEFCoreDbExecutor ??
                                throw new ArgumentException($"Current DbExecutor can not assignable from {nameof(IEFCoreDbExecutor)}");

            CheckValue.NotNull(efDbExecutor.EFCoreDbContext, nameof(efDbExecutor.EFCoreDbContext));

            return (efDbExecutor.EFCoreDbContext, efDbExecutor.ShouldCommitTrxForEFCore);
        }
    }
}
