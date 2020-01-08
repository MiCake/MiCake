using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// this is a ITransactionFeature base ef framework
    /// </summary>
    internal class EFTransactionFeature : ITransactionFeature
    {
        public bool IsCommit { get; private set; }
        public bool IsRollback { get; private set; }
        public bool IsOpenTransaction => _isOpenTransaction;
        public DbContext DbContext { get; private set; }

        private bool _isOpenTransaction;
        private bool _isDispose;
        private IDbContextTransaction _dbContextTransaction;

        public EFTransactionFeature(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public void SetTransaction(IDbContextTransaction dbContextTransaction)
        {
            // [todo] : need throw exception when repeat settings.
            //if (_isOpenTransaction)
            //    throw new MiCakeException("this transaction feature is already set transaction!");

            _isOpenTransaction = true;
            _dbContextTransaction = dbContextTransaction;
        }

        public void UseAmbientTransaction()
        {
            // if there find ambient transaction.
            // forexample : there have a transaction scope.

            _isOpenTransaction = true;
        }

        public void Commit()
        {
            if (IsCommit)
                return;

            IsCommit = true;

            DbContext.SaveChanges();
            _dbContextTransaction?.Commit();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsCommit)
                return;

            IsCommit = true;

            await DbContext.SaveChangesAsync();
            await _dbContextTransaction?.CommitAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (_isDispose)
                return;

            _isDispose = true;

            _dbContextTransaction?.Dispose();
            DbContext?.Dispose();
        }

        public void Rollback()
        {
            if (IsRollback)
                return;

            IsRollback = true;

            _dbContextTransaction?.Rollback();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (IsRollback)
                return;

            IsRollback = true;

            await _dbContextTransaction?.RollbackAsync(cancellationToken);
        }
    }
}
