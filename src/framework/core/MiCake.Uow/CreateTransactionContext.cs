namespace MiCake.Uow
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

        public CreateTransactionContext(IUnitOfWork unitOfWork)
        {
            CurrentUnitOfWork = unitOfWork;
        }
    }
}
