using MiCake.Core.Data;
using Microsoft.Extensions.Logging;

namespace MiCake.Uow.Internal
{
    internal abstract class BaseUnitOfWork : IUnitOfWorkNode, IHasSupplement<Action<IUnitOfWorkNode>>
    {
        protected bool _isSaved = false;
        protected bool _isRollback = false;
        protected Action<IUnitOfWorkNode>? _coreNodeDisposeHandler;

        public IUnitOfWorkNode? Parent { get; set; }

        public void SetData(Action<IUnitOfWorkNode> parts)
        {
            _coreNodeDisposeHandler = parts;
        }

        protected void CheckSaved()
        {
            if (_isSaved)
                throw new InvalidOperationException($"This unit of work has been saved!");
        }

        protected void CheckRollback()
        {
            if (_isRollback)
                throw new InvalidOperationException($"This unit of work has been rollback!");
        }

        protected static void ReThrow(IEnumerable<Exception> exceptions)
        {
            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }

    internal class UnitOfWork : BaseUnitOfWork, IUnitOfWork
    {
        public Guid ID { get; }
        public bool IsDisposed { get; private set; }
        public UnitOfWorkOptions UnitOfWorkOptions { get; set; } = new();
        public IServiceProvider ServiceProvider { get; private set; }

        private readonly IEnumerable<ITransactionProvider> _transactionProviders;

        protected List<ITransactionObject> ErrorTransactions { get; private set; } = new();
        protected List<ITransactionObject> CreatedTransactions { get; private set; } = new();

        public List<IUnitOfWorkInterceptor> Interceptors { get; } = new();

        private List<IUnitOfWorkCompletedInterceptor> CompletedInterceptors => Interceptors.OfType<IUnitOfWorkCompletedInterceptor>().ToList();
        private List<IUnitOfWorkRollbackedInterceptor> RollbackedInterceptors => Interceptors.OfType<IUnitOfWorkRollbackedInterceptor>().ToList();

        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(IServiceProvider serviceProvider, IEnumerable<ITransactionProvider> transactionProviders, ILoggerFactory loggerFactory)
        {
            ID = Guid.NewGuid();
            ServiceProvider = serviceProvider;

            _transactionProviders = transactionProviders.OrderBy(s => s.Order);
            _logger = loggerFactory.CreateLogger<UnitOfWork>();

            TryOpenTransaction();
        }

        public void TryOpenTransaction()
        {
            List<Exception> _errors = new();

            foreach (var transactionProvider in _transactionProviders)
            {
                try
                {
                    var currentTransaction = transactionProvider.GetTransactionObject();
                    CreatedTransactions.Add(currentTransaction);
                }
                catch (Exception ex)
                {
                    _errors.Add(ex);
                }
            }

            if (_errors.Count > 0)
            {
                CreatedTransactions.Clear();
                ReThrow(_errors);
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            //release transaction.
            foreach (var transaction in CreatedTransactions)
            {
                transaction.Dispose();
            }

            if (_coreNodeDisposeHandler == null)
            {
                throw new InvalidOperationException($"There is no core dispose handler for unit of work [{ID}]. Please check your unit of work configuration.");
            }
            _coreNodeDisposeHandler.Invoke(this);
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

                    _logger.LogError(ex, "Uow SaveChangesAsync failed.");
                }
            }

            if (exceptions.Count > 0)
                ReThrow(exceptions);

            foreach (var item in CompletedInterceptors)
            {
                await item.Completed(this, cancellationToken);
            }

            _isSaved = true;
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

            //if rollback has error, throw exception directly.

            foreach (var item in RollbackedInterceptors)
            {
                await item.Rollbacked(this, cancellationToken);
            }

            _isRollback = true;
        }

        public List<ITransactionObject> GetTransactionObjects()
        {
            return CreatedTransactions.Select(s => s).ToList();
        }
    }
}
