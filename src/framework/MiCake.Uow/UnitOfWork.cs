using JetBrains.Annotations;
using MiCake.Uow.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    public class UnitOfWork : IUnitOfWork, IUnitOfWorkHook
    {
        public Guid ID { get; private set; }
        public bool IsDisposed { get; private set; }
        public event EventHandler<IUnitOfWork> DisposeHandler;
        public UnitOfWorkOptions UnitOfWorkOptions { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        private readonly Dictionary<string, ITransactionFeature> _transactionFeatures;
        private Action _saveChangedAction;
        private Action _rollBackedAction;
        private bool _isSaveChanged;
        private bool _isRollbacked;

        public UnitOfWork(
            IOptions<UnitOfWorkDefaultOptions> defaultOptions,
            IServiceProvider serviceProvider)
        {
            if (defaultOptions.Value == null)
                throw new ArgumentNullException($"can not get default UnitOfWorkDefaultOptions!");

            ID = Guid.NewGuid();
            UnitOfWorkOptions = defaultOptions.Value.ConvertToUnitOfWorkOptions();
            ServiceProvider = serviceProvider;

            _transactionFeatures = new Dictionary<string, ITransactionFeature>();
        }

        public virtual ITransactionFeature GetOrAddTransactionFeature(
            [NotNull]string key,
            [NotNull] ITransactionFeature transcationFeature)
        {
            if (_transactionFeatures.ContainsKey(key))
                return _transactionFeatures.GetValueOrDefault(key);

            _transactionFeatures.Add(key, transcationFeature);
            return transcationFeature;
        }

        public virtual ITransactionFeature GetTransactionFeature([NotNull]string key)
        {
            return _transactionFeatures.GetValueOrDefault(key);
        }

        public virtual void RemoveTransaction([NotNull]string key)
        {
            _transactionFeatures.Remove(key);
        }

        public virtual void RegisteTransactionFeature(
            [NotNull]string key,
            [NotNull]ITransactionFeature transcationFeature)
        {
            if (_transactionFeatures.ContainsKey(key))
                return;

            _transactionFeatures.Add(key, transcationFeature);
        }

        public virtual void Rollback()
        {
            if (_isRollbacked)
                throw new InvalidOperationException("this unit work is already  execute Rollback method.");

            var rollBackExceptions = new List<Exception>();

            _isRollbacked = true;

            foreach (var transactionFeature in _transactionFeatures.Values)
            {
                try
                {
                    transactionFeature.Rollback();
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during savechanges
                    rollBackExceptions.Add(ex);
                }
            }

            if (rollBackExceptions.Count > 0)
                throw new AggregateException(rollBackExceptions);

            _rollBackedAction?.Invoke();
        }

        public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_isRollbacked)
                throw new InvalidOperationException("this unit work is already  execute Rollback method.");

            var rollBackExceptions = new List<Exception>();

            _isRollbacked = true;

            foreach (var transactionFeature in _transactionFeatures.Values)
            {
                try
                {
                    await transactionFeature.RollbackAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during savechanges
                    rollBackExceptions.Add(ex);
                }
            }

            if (rollBackExceptions.Count > 0)
                throw new AggregateException(rollBackExceptions);

            _rollBackedAction?.Invoke();
        }

        public virtual void SaveChanges()
        {
            if (_isSaveChanged)
                throw new InvalidOperationException("this unit work is already  execute SaveChanges method.");

            var saveExceptions = new List<Exception>();

            _isSaveChanged = true;

            foreach (var transactionFeature in _transactionFeatures.Values)
            {
                try
                {
                    transactionFeature.Commit();
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during savechanges
                    saveExceptions.Add(ex);
                }
            }

            if (saveExceptions.Count > 0)
                throw new AggregateException(saveExceptions);

            _saveChangedAction?.Invoke();
        }

        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_isSaveChanged)
                throw new InvalidOperationException("this unit work is already  execute SaveChanges method.");

            var saveExceptions = new List<Exception>();

            _isSaveChanged = true;

            foreach (var transactionFeature in _transactionFeatures.Values)
            {
                try
                {
                    await transactionFeature.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during savechanges
                    saveExceptions.Add(ex);
                }
            }

            if (saveExceptions.Count > 0)
                throw new AggregateException(saveExceptions);

            _saveChangedAction?.Invoke();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            var disposeExceptions = new List<Exception>();

            foreach (var transactionFeature in _transactionFeatures.Values)
            {
                try
                {
                    transactionFeature.Dispose();
                }
                catch (Exception ex)
                {
                    disposeExceptions.Add(ex);
                }
            }

            if (disposeExceptions.Count > 0)
                throw new AggregateException(disposeExceptions);

            DisposeHandler.Invoke(this, this);
        }

        public virtual void OnSaveChanged(Action action)
        {
            _saveChangedAction += action;
        }

        public virtual void OnRollBacked(Action action)
        {
            _rollBackedAction += action
                ;
        }

        public void SetOptions(UnitOfWorkOptions options)
        {
            UnitOfWorkOptions = options.Clone();
        }
    }
}
