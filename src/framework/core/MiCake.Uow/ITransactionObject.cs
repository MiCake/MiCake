namespace MiCake.Uow
{
    /// <summary>
    /// Defining a transaction object will be used in the <see cref="IUnitOfWork"/>
    /// </summary>
    public interface ITransactionObject : IDisposable
    {
        /// <summary>
        /// The ID of <see cref="ITransactionObject"/>.
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// Indicates whether the current transaction has been committed
        /// </summary>
        public bool IsCommit { get; }

        /// <summary>
        /// The type of transaction object.
        /// </summary>
        public Type TransactionType { get; }

        /// <summary>
        /// Current transaction instance.
        /// </summary>
        public object TransactionInstance { get; }

        /// <summary>
        /// Commits all changes made to the database in the current transaction asynchronously.
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///  Discards all changes made to the database in the current transaction asynchronously.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
