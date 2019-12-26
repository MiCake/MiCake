using JetBrains.Annotations;

namespace MiCake.Uow
{
    /// <summary>
    /// a <see cref="ITransactionFeature"/> container.
    /// </summary>
    public interface ITransactionFeatureContainer
    {
        void RegisteTranasctionFeature([NotNull]string key, [NotNull]ITransactionFeature transactionFeature);

        ITransactionFeature GetOrAddTransactionFeature([NotNull]string key, [NotNull]ITransactionFeature transactionFeature);

        ITransactionFeature GetTransactionFeature([NotNull]string key);

        void RemoveTransaction([NotNull]string key);
    }
}
