namespace MiCake.MessageBus
{
    /// <summary>
    /// A provider for <see cref="IMessageSubscriber"/>.
    /// </summary>
    public interface IMessageSubscribeFactory
    {
        /// <summary>
        /// Create new <see cref="IMessageSubscriber"/>.
        /// </summary>
        /// <returns></returns>
        IMessageSubscriber CreateSubscriber();
    }
}
