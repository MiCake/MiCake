using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;

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

            var cacheDbContext = currentUow.GetTransactionFeature(_key);
            if (cacheDbContext != null)
                return (TDbContext)((EFTransactionFeature)cacheDbContext).DbContext;

            var wantedDbContext = (TDbContext)currentUow.ServiceProvider.GetService(typeof(TDbContext));

            if (wantedDbContext == null)
                throw new NullReferenceException("Cannot get DbContext.Please check add ef services correctly");

            AddDbTransactionFeatureToUow(currentUow, wantedDbContext);

            return wantedDbContext;

        }

        private void AddDbTransactionFeatureToUow(IUnitOfWork uow, TDbContext dbContext)
        {
            var efFeature = (EFTransactionFeature)uow.GetOrAddTransactionFeature(_key, new EFTransactionFeature(dbContext));

            //todo : if there have transaction scope. need set this feature UseAmbientTransaction;

            if (IsFeatureNeedOpenTransaction(uow, efFeature))
            {
                var dbcontextTransaction = uow.UnitOfWorkOptions.IsolationLevel.HasValue ?
                                            dbContext.Database.BeginTransaction(uow.UnitOfWorkOptions.IsolationLevel.Value) :
                                            dbContext.Database.BeginTransaction();

                efFeature.SetTransaction(dbcontextTransaction);
            }
        }

        private bool IsFeatureNeedOpenTransaction(IUnitOfWork uow, EFTransactionFeature efFeature)
        {
            return uow.UnitOfWorkOptions.Limit != UnitOfWorkLimit.Suppress && !efFeature.IsOpenTransaction;
        }
    }
}
