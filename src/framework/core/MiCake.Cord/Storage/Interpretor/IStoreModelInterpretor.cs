namespace MiCake.Cord.Storage.Interpretor
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// <para>
    ///     Provide a solution to explain <see cref="IStoreModel"/>
    /// </para>
    /// </summary>
    public interface IStoreModelInterpretor : IDisposable
    {
        /// <summary>
        /// This is an internal API  not subject to the same compatibility standards as public APIs.
        /// It may be changed or removed without notice in any release.
        /// 
        /// <para>
        ///    Explain to the receiver.
        /// </para>
        /// </summary>
        void Interpret<TReceiver>(IStoreModel storeModel, TReceiver receiver);
    }
}
