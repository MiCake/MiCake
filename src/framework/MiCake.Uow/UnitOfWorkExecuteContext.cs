namespace MiCake.Uow
{
    public class UnitOfWorkExecutionContext
    {
        /// <summary>
        /// Current unit of work.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork { get; private set; }

        /// <summary>
        /// Current transaction.
        /// </summary>
        public ITransactionObject CurrentTransaction { get; private set; }

        public UnitOfWorkExecutionContext(IUnitOfWork unitOfWork, ITransactionObject transactionObject)
        {
            CurrentUnitOfWork = unitOfWork;
            CurrentTransaction = transactionObject;
        }
    }
}
