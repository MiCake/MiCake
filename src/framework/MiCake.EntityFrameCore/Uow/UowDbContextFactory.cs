using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class UowDbContextFactory<TDbContext> : IUowDbContextFactory<TDbContext>
         where TDbContext : DbContext
    {
        private readonly IUnitOfWorkManager _uowManager;
        private string _key => $"EFCore - {typeof(TDbContext).FullName}";

        public UowDbContextFactory(IUnitOfWorkManager uowManager)
        {
            _uowManager = uowManager;
        }

        public TDbContext CreateDbContext()
        {
            var currentUow = _uowManager.GetCurrentUnitOfWork();

            if (currentUow == null)
                throw new NullReferenceException("Cannot get a unit of work,Please check create root unit of work correctly");

            var transactionFeature = currentUow.GetTransactionFeature(_key);
            if (transactionFeature != null && transactionFeature is EFTransactionFeature efTransacationFeature)
            {
                var cacheDbContext = (TDbContext)efTransacationFeature.DbContext;
                if (!efTransacationFeature.IsOpenTransaction)
                {
                    //is need open transaction
                    OpenTransaction(efTransacationFeature, currentUow, cacheDbContext);
                }

                return cacheDbContext;
            }

            var wantedDbContext = (TDbContext)currentUow.ServiceProvider.GetService(typeof(TDbContext));

            if (wantedDbContext == null)
                throw new NullReferenceException("Cannot get DbContext.Please check add ef services correctly");

            var newEFTransactionFeature = new EFTransactionFeature(wantedDbContext);
            currentUow.GetOrAddTransactionFeature(_key, newEFTransactionFeature);

            OpenTransaction(newEFTransactionFeature, currentUow, wantedDbContext);

            return wantedDbContext;

        }

        private void OpenTransaction(
            EFTransactionFeature eFTransactionFeature,
            IUnitOfWork uow,
            TDbContext dbContext,
            Func<bool> externalRule = null)
        {
            var externalCheckResult = externalRule == null ? true : externalRule.Invoke();

            if (InternalOpenRuleCheck(eFTransactionFeature, uow) && externalCheckResult)
            {
                Debug.Print("open a transaction.");

                var dbcontextTransaction = uow.UnitOfWorkOptions.IsolationLevel.HasValue ?
                                            dbContext.Database.BeginTransaction(uow.UnitOfWorkOptions.IsolationLevel.Value) :
                                            dbContext.Database.BeginTransaction();

                eFTransactionFeature.SetTransaction(dbcontextTransaction);
            }

            bool InternalOpenRuleCheck(EFTransactionFeature efFeature, IUnitOfWork uow)
              => uow.UnitOfWorkOptions.Scope != UnitOfWorkScope.Suppress && !efFeature.IsOpenTransaction;
        }
    }
}
