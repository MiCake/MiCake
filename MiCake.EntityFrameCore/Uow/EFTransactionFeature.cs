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

        private bool _isOpenTransaction;
        private bool _isDispose;
        private IDbContextTransaction _dbContextTransaction;
        private DbContext _dbContext;

        public EFTransactionFeature(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void SetTransaction(IDbContextTransaction dbContextTransaction)
        {

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

            _dbContext.SaveChanges();
            _dbContextTransaction?.Commit();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsCommit)
                return;

            IsCommit = true;

            await _dbContext.SaveChangesAsync();
            await _dbContextTransaction?.CommitAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (_isDispose)
                return;

            _isDispose = true;

            _dbContextTransaction?.Dispose();
            _dbContext?.Dispose();
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
