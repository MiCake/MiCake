using MiCake.DDD.Connector.Store.Configure;

namespace MiCake.DDD.Connector.Store.Interpret
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// <para>
    ///     Provide a solution to explain <see cref="IStoreModel"/>
    /// </para>
    /// </summary>
    public interface IStoreModelExpression
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
