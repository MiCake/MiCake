using MiCake.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Internal
{
    public class UnitOfWork : IUnitOfWork, INeedParts<UnitOfWorkNeedParts>
    {
        public Guid ID { get; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions { get; private set; }
        public IServiceScope ServiceScope { get; private set; }

        private UnitOfWorkEvents Events;
        private readonly IEnumerable<ITransactionProvider> _transactions;
        private readonly IEnumerable<IUnitOfWorkExecutor> _executors;

        private bool _isSaved = false;
        private bool _isRollback = false;
        private List<ITransactionObject> _errorTransactions = new List<ITransactionObject>();
        private List<ITransactionObject> _createdTransactions = new List<ITransactionObject>();

        /// <summary>
        /// Collections that <see cref="IUnitOfWorkExecutor"/> have not yet been executed in this unit of work.
        /// </summary>
        protected IEnumerable<IUnitOfWorkExecutor> NeedExecutionTasks
        {
            get => _executors.Count() == 0 ? _executors : _executors.Where(s => !s.HasExecuted);
        }

        public UnitOfWork(
            IEnumerable<ITransactionProvider> transactions,
            IEnumerable<IUnitOfWorkExecutor> executors)
        {
            _transactions = transactions.OrderBy(s => s.Order);
            _executors = executors;

            ID = Guid.NewGuid();
        }

        public void Dispose()
        {
            if (IsDisposed)
                throw new InvalidOperationException($"This {nameof(UnitOfWork)} has been disposed!");

            IsDisposed = true;

            //release db executor.
            foreach (var dbExecutor in _executors)
            {
                dbExecutor.Dispose();
            }

            //release transaction.
            foreach (var transaction in _createdTransactions)
            {
                transaction.Dispose();
            }
        }

        public void Rollback()
        {
            CheckRollback();

            //error transaction have priority.
            foreach (var errorTransaction in _errorTransactions)
            {
                errorTransaction.Rollback();
            }

            foreach (var createTransaction in _createdTransactions)
            {
                if (createTransaction.IsOpened)
                    createTransaction.Rollback();
            }

            //if rollback has error, throw exception to developer.
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            CheckRollback();

            //error transaction have priority.
            foreach (var errorTransaction in _errorTransactions)
            {
                await errorTransaction.RollbackAsync(cancellationToken);
            }

            foreach (var createTransaction in _createdTransactions)
            {
                if (createTransaction.IsOpened)
                    await createTransaction.RollbackAsync(cancellationToken);
            }

            //if rollback has error, throw exception to developer.
        }

        public void SaveChanges()
        {
            CheckSaved();

            if (NeedExecutionTasks.Count() == 0)
                return;

            var exceptions = new List<Exception>();

            if (_transactions.Count() == 0)
            {
                //no transaction.direct execution.
                //because there is no transaction management, there is no way to roll back even if there is an error
                ExecuteTaskWithNoTransaction(exceptions);
            }
            else
            {
                bool hasError = false;

                foreach (var transactionProvider in _transactions)
                {
                    var transactionObj = transactionProvider.GetTransaction();
                    var executionContext = new UnitOfWorkExecutionContext(this, transactionObj);

                    var belongTransactionExecutor = NeedExecutionTasks.Where(s => s.CanExecute(executionContext)).ToList();

                    if (belongTransactionExecutor.Count == 0)
                        continue;

                    //open transaction
                    using (var @transaction = transactionObj.Open())
                    {
                        _createdTransactions.Add(@transaction);

                        foreach (var executionTask in belongTransactionExecutor)
                        {
                            if (ExecuteDb(executionTask, exceptions).Result == UnitOfWorkResultType.Failed)
                            {
                                hasError = true;
                                break;
                            }
                        }

                        if (!hasError)
                        {
                            @transaction.Commit();
                        }
                        else
                        {
                            //if has error,add error transation to _errorTrasacations.
                            //this transaction is delayed until the unit of work call Dispose.
                            _errorTransactions.Add(@transaction);
                            break;
                        }
                    }
                }

                if (!hasError)
                {
                    //execute remain task with no transaction.
                    ExecuteTaskWithNoTransaction(exceptions);
                }
            }

            if (exceptions.Count > 0)
                ReThrow(exceptions);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CheckSaved();

            if (NeedExecutionTasks.Count() == 0)
                return;

            var exceptions = new List<Exception>();

            if (_transactions.Count() == 0)
            {
                //no transaction.direct execution.
                //because there is no transaction management, there is no way to roll back even if there is an error
                await ExecuteTaskWithNoTransactionAsync(exceptions, cancellationToken);
            }
            else
            {
                bool hasError = false;

                foreach (var transactionProvider in _transactions)
                {
                    var transactionObj = transactionProvider.GetTransaction();
                    var executionContext = new UnitOfWorkExecutionContext(this, transactionObj);

                    var belongTransactionExecutor = NeedExecutionTasks.Where(s => s.CanExecute(executionContext)).ToList();

                    if (belongTransactionExecutor.Count == 0)
                        continue;

                    //open transaction
                    using (var @transaction = transactionObj.Open())
                    {
                        _createdTransactions.Add(@transaction);

                        foreach (var executionTask in belongTransactionExecutor)
                        {
                            var execiteResult = await ExecuteDbAsync(executionTask, exceptions, cancellationToken);
                            if (execiteResult.Result == UnitOfWorkResultType.Failed)
                            {
                                hasError = true;
                                break;
                            }
                        }

                        if (!hasError)
                        {
                            await @transaction.CommitAsync(cancellationToken);
                        }
                        else
                        {
                            //if has error,add error transation to _errorTrasacations.
                            //this transaction is delayed until the unit of work call Dispose.
                            _errorTransactions.Add(@transaction);
                            break;
                        }
                    }
                }

                if (!hasError)
                {
                    //execute remain task with no transaction.
                    await ExecuteTaskWithNoTransactionAsync(exceptions, cancellationToken);
                }
            }

            if (exceptions.Count > 0)
                ReThrow(exceptions);
        }

        #region Private Method
        private void CheckSaved()
        {
            if (_isSaved)
                throw new InvalidOperationException($"This unit of work has been saved!");
        }

        private void CheckRollback()
        {
            if (_isRollback)
                throw new InvalidOperationException($"This unit of work has been rollback!");
        }

        /// <summary>
        /// Execute all need execution <see cref="IUnitOfWorkExecutor"/> with no transaction.
        /// </summary>
        private void ExecuteTaskWithNoTransaction(List<Exception> exceptions)
        {
            foreach (var remainTask in NeedExecutionTasks)
            {
                ExecuteDb(remainTask, exceptions);
            }
        }

        /// <summary>
        /// Execute all need execution <see cref="IUnitOfWorkExecutor"/> with no transaction.
        /// </summary>
        private async Task ExecuteTaskWithNoTransactionAsync(List<Exception> exceptions, CancellationToken cancellationToken)
        {
            foreach (var remainTask in NeedExecutionTasks)
            {
                await ExecuteDbAsync(remainTask, exceptions, cancellationToken);
            }
        }

        private UnitOfWorkExecutionResult ExecuteDb(IUnitOfWorkExecutor executor, List<Exception> exceptions)
        {
            var result = executor.Execute();

            if (result.Result == UnitOfWorkResultType.Failed)
                exceptions.Add(result.Failure);

            return result;
        }

        private async Task<UnitOfWorkExecutionResult> ExecuteDbAsync(IUnitOfWorkExecutor executor, List<Exception> exceptions, CancellationToken cancellationToken)
        {
            var result = await executor.ExecuteAsync(cancellationToken);

            if (result.Result == UnitOfWorkResultType.Failed)
                exceptions.Add(result.Failure);

            return result;
        }

        private void ReThrow(IEnumerable<Exception> exceptions)
        {
            if (exceptions.Count() > 0)
                throw new AggregateException(exceptions);
        }
        #endregion

        public void SetParts(UnitOfWorkNeedParts parts)
        {
            UnitOfWorkOptions = parts.Options;
            ServiceScope = parts.ServiceScope;
        }
    }
}
