using MiCake.Core.Data;
using MiCake.Core.Util;
using MiCake.Core.Util.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow.Internal
{
    internal class UnitOfWork : IUnitOfWork, IDependencyReceiver<UowDependencyParts>
    {
        public Guid ID { get; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions { get; private set; }
        public IServiceScope ServiceScope { get; private set; }

        private readonly IEnumerable<ITransactionProvider> _transactions;

        protected UnitOfWorkEvents Events => UnitOfWorkOptions?.Events;
        protected List<ITransactionObject> ErrorTransactions { get; private set; } = new();
        protected List<ITransactionObject> CreatedTransactions { get; private set; } = new();
        protected List<IDbExecutor> AddedExecutors { get; private set; } = new();
        protected Action<IUnitOfWork> DisposeHandler { get; private set; }

        private bool _isSaved = false;
        private bool _isRollback = false;

        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(IEnumerable<ITransactionProvider> transactions, ILoggerFactory loggerFactory)
        {
            _transactions = transactions.OrderBy(s => s.Order);

            ID = Guid.NewGuid();
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            //release db executor.
            foreach (var dbExecutor in AddedExecutors)
            {
                dbExecutor.Dispose();
            }

            //release transaction.
            foreach (var transaction in CreatedTransactions)
            {
                transaction.Dispose();
            }

            //pop this unit of work to stack.
            DisposeHandler?.Invoke(this);
        }

        public async Task<bool> TryAddDbExecutorAsync(IDbExecutor dbExecutor, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNull(dbExecutor, nameof(dbExecutor));

            //added executor.Guarantee that it will eventually be released
            AddedExecutors.AddIfNotContains(dbExecutor);

            if (!EnsureToOpenTransaction())
                return true;

            if (dbExecutor.HasTransaction)
                return true;

            bool result = false;
            var transactionObj = await OpenTransactionAndGiveExecutorAsync(_transactions, dbExecutor, cancellationToken);

            if (transactionObj != null)
            {
                //reused or new?
                if (!CreatedTransactions.Any(s => s.ID == transactionObj.ID))
                    CreatedTransactions.Add(transactionObj);

                result = true;
            }

            return result;
        }

        protected virtual async Task<ITransactionObject> OpenTransactionAndGiveExecutorAsync(
            IEnumerable<ITransactionProvider> transactionProviders,
            IDbExecutor dbExecutor,
            CancellationToken cancellationToken)
        {
            //if not any provider,reutrn false.
            if (!transactionProviders.Any())
                return null;

            ITransactionObject exceptedTransaction = null;

            foreach (var transactionProvider in transactionProviders)
            {
                if (!transactionProvider.CanCreate(dbExecutor))
                    continue;

                //already has transaction,can reused?
                if (CreatedTransactions.Count > 0)
                {
                    var reusedTransaction = transactionProvider.Reused(CreatedTransactions, dbExecutor);

                    if (reusedTransaction != null)
                    {
                        //reused this transaction,and back.
                        await dbExecutor.UseTransactionAsync(reusedTransaction, cancellationToken);
                        return reusedTransaction;
                    }
                }

                //create new transaction.
                exceptedTransaction = await transactionProvider.GetTransactionObjectAsync(new CreateTransactionContext(this, dbExecutor), cancellationToken);
                if (exceptedTransaction != null)
                {
                    await dbExecutor.UseTransactionAsync(exceptedTransaction, cancellationToken);
                    break;
                }
                //if exceptedTransaction is null,let the next provider create.
            }

            return exceptedTransaction;
        }

        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CheckSaved();

            List<Exception> exceptions = new();

            foreach (var @transaction in CreatedTransactions)
            {
                try
                {
                    await @transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    ErrorTransactions.Add(@transaction);
                    exceptions.Add(ex);

                    _logger.LogError(ex, "unit of work SaveChangesAsync failed.");
                }
            }

            if (exceptions.Count > 0)
                ReThrow(exceptions);

            _isSaved = true;

            await Events?.Completed(this);
        }

        public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            CheckRollback();

            //error transaction have priority.
            foreach (var errorTransaction in ErrorTransactions)
            {
                await errorTransaction.RollbackAsync(cancellationToken);
            }

            foreach (var createTransaction in CreatedTransactions)
            {
                if (!createTransaction.IsCommit)
                    await createTransaction.RollbackAsync(cancellationToken);
            }

            //if rollback has error, throw exception to developer.

            _isRollback = true;

            await Events?.Rollbacked(this);
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

        private static void ReThrow(IEnumerable<Exception> exceptions)
        {
            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        //If config scope is Suppress,Transaction will not be opened
        private bool EnsureToOpenTransaction()
            => UnitOfWorkOptions.Scope != UnitOfWorkScope.Suppress;
        #endregion

        public void AddDependency(UowDependencyParts parts)
        {
            UnitOfWorkOptions = parts.Options;
            ServiceScope = parts.ServiceScope;
            DisposeHandler = parts.DisposeHandler;
        }
    }
}
