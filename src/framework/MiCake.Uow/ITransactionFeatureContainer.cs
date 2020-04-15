

namespace MiCake.Uow
{
    /// <summary>
    /// a <see cref="ITransactionFeature"/> container.
    /// </summary>
    public interface ITransactionFeatureContainer
    {
        void RegisteTransactionFeature(string key, ITransactionFeature transactionFeature);

        ITransactionFeature GetOrAddTransactionFeature(string key, ITransactionFeature transactionFeature);

        ITransactionFeature GetTransactionFeature(string key);

        void RemoveTransaction(string key);
    }
}
