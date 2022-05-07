using MiCake.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Internal
{
    internal class ChildUnitOfWork : IChildUnitOfWork, IHasSupplement<UnitOfWorkNeedParts>
    {
        public Guid ID { get; private set; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions { get; private set; }
        public IServiceScope ServiceScope => ParentUow.ServiceScope;

        protected IUnitOfWork ParentUow { get; private set; }
        protected UnitOfWorkEvents Events => UnitOfWorkOptions?.Events;
        protected Action<IUnitOfWork> DisposeHandler { get; private set; }

        private bool _isSaveChanged;
        private bool _isRollbacked;


        public ChildUnitOfWork(IUnitOfWork parentUow)
        {
            ParentUow = parentUow;
            ID = Guid.NewGuid();
        }

        public void Dispose()
        {
            if (IsDisposed)
                throw new InvalidOperationException("this unit of work is already dispose");

            IsDisposed = true;

            //pop this unit of work to stack.
            DisposeHandler?.Invoke(this);
        }

        #region these mothod will be performed by the parent unit of work
        public void Rollback()
        {
            if (_isRollbacked)
                throw new InvalidOperationException("this unit work is already  execute Rollback method.");

            _isRollbacked = true;

            Events?.Rollbacked(this);
            //Do nothing.
            //This operation will be performed by the parent unit of work
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Rollback();

            Events?.Rollbacked(this);

            return Task.FromResult(0);
        }

        public void SaveChanges()
        {
            if (_isSaveChanged)
                throw new InvalidOperationException("this unit work is already  execute SaveChanges method.");

            _isSaveChanged = true;

            Events?.Completed(this);

            //Do nothing.
            //This operation will be performed by the parent unit of work
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChanges();

            Events?.Completed(this);

            return Task.FromResult(0);
        }
        #endregion

        public IUnitOfWork GetParentUnitOfWork()
            => ParentUow;

        public void SetData(UnitOfWorkNeedParts parts)
        {
            //Although the options is set, the options of the parent unit of work is still used.
            //Just to get Events and disposeHandler.
            UnitOfWorkOptions = parts.Options;
            DisposeHandler = parts.DisposeHandler;
        }

        public async Task<bool> TryAddDbExecutorAsync(IDbExecutor dbExecutor, CancellationToken cancellationToken = default)
            => await ParentUow.TryAddDbExecutorAsync(dbExecutor, cancellationToken);
    }
}
