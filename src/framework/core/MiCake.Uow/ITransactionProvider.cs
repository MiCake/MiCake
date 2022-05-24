namespace MiCake.Uow
{
    /// <summary>
    /// A provider for <see cref="ITransactionObject"/>.
    /// </summary>
    public interface ITransactionProvider
    {
        /// Gets the order value for determining the order of execution of providers.
        /// Providers execute in ascending numeric value of the <see cref="Order"/> property.
        public int Order { get; }

        /// <summary>
        /// Get a <see cref="ITransactionObject"/>.
        /// </summary>
        ITransactionObject GetTransactionObject();
    }
}
