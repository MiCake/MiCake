using MiCake.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    internal class ChildUnitOfWork : IChildUnitOfWork, INeedParts<UnitOfWorkNeedParts>
    {
        public Guid ID { get; private set; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions { get; private set; }
        public IServiceScope ServiceScope => _parentUow.ServiceScope;

        private UnitOfWorkEvents Events;
        private IUnitOfWork _parentUow;
        private bool _isSaveChanged;
        private bool _isRollbacked;


        public ChildUnitOfWork(IUnitOfWork parentUow)
        {
            _parentUow = parentUow;
            ID = Guid.NewGuid();
        }

        public void Dispose()
        {
            if (IsDisposed)
                throw new InvalidOperationException("this unit of work is already dispose");

            IsDisposed = true;
        }

        #region these mothod will be performed by the parent unit of work
        public void Rollback()
        {
            if (_isRollbacked)
                throw new InvalidOperationException("this unit work is already  execute Rollback method.");

            _isRollbacked = true;

            //Do nothing.
            //This operation will be performed by the parent unit of work
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Rollback();

            return Task.FromResult(0);
        }

        public void SaveChanges()
        {
            if (_isSaveChanged)
                throw new InvalidOperationException("this unit work is already  execute SaveChanges method.");

            _isSaveChanged = true;

            //Do nothing.
            //This operation will be performed by the parent unit of work
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChanges();

            return Task.FromResult(0);
        }
        #endregion

        public IUnitOfWork GetParentUnitOfWork()
            => _parentUow;

        public void SetParts(UnitOfWorkNeedParts parts)
        {
            //Although the options is set, the options of the parent unit of work is still used.
            //Just to get Events.
            UnitOfWorkOptions = parts.Options;
        }

        public bool TryAddDbExecutor(IDbExecutor dbExecutor)
            => _parentUow.TryAddDbExecutor(dbExecutor);

        public async Task<bool> TryAddDbExecutorAsync(IDbExecutor dbExecutor, CancellationToken cancellationToken = default)
            => await _parentUow.TryAddDbExecutorAsync(dbExecutor, cancellationToken);
    }
}
