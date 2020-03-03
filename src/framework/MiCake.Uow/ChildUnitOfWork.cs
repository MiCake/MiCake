using JetBrains.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    internal class ChildUnitOfWork : IChildUnitOfWork, IUnitOfWorkHook
    {
        public Guid ID { get; private set; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions => _parentUow.UnitOfWorkOptions;
        public IServiceProvider ServiceProvider => _parentUow.ServiceProvider;

        public event EventHandler<IUnitOfWork> DisposeHandler;

        private IUnitOfWork _parentUow;
        private bool _isSaveChanged;
        private bool _isRollbacked;

        public ChildUnitOfWork([NotNull]IUnitOfWork parentUow)
        {
            _parentUow = parentUow;
            ID = Guid.NewGuid();
        }

        public void Dispose()
        {
            if (IsDisposed)
                throw new InvalidOperationException("this unit of work is already dispose");

            IsDisposed = true;

            DisposeHandler?.Invoke(this, this);

            _parentUow = null;
        }

        public ITransactionFeature GetOrAddTransactionFeature(
            [NotNull] string key,
            [NotNull] ITransactionFeature transactionFeature)
        {
            return _parentUow.GetOrAddTransactionFeature(key, transactionFeature);
        }

        public ITransactionFeature GetTransactionFeature([NotNull] string key)
        {
            return _parentUow.GetTransactionFeature(key);
        }

        public void RegisteTransactionFeature(
            [NotNull] string key,
            [NotNull] ITransactionFeature transactionFeature)
        {
            _parentUow.RegisteTransactionFeature(key, transactionFeature);
        }

        public void RemoveTransaction([NotNull] string key)
        {
            _parentUow.RemoveTransaction(key);
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
        {
            return _parentUow;
        }

        public void OnSaveChanged(Action action)
        {
            (_parentUow as IUnitOfWorkHook)?.OnSaveChanged(action);
        }

        public void OnRollBacked(Action action)
        {
            (_parentUow as IUnitOfWorkHook)?.OnRollBacked(action);
        }

        /// <summary>
        /// Child unit of work invalid settings.because it's use parent options
        /// </summary>
        /// <param name="options"></param>
        public void SetOptions(UnitOfWorkOptions options)
        {
            //_parentUow.SetOptions(options);
        }
    }
}
