namespace MiCake.DDD.Uow
{
    /// <summary>
    /// A context when create <see cref="ITransactionObject"/>.
    /// </summary>
    public class CreateTransactionContext
    {
        /// <summary>
        /// Current unit of work.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork { get; private set; }

        /// <summary>
        /// Current <see cref="IDbExecutor"/>.
        /// </summary>
        public IDbExecutor CurrentDbExecutor { get; private set; }

        public CreateTransactionContext(IUnitOfWork unitOfWork, IDbExecutor dbExecutor)
        {
            CurrentUnitOfWork = unitOfWork;
            CurrentDbExecutor = dbExecutor;
        }
    }
}
