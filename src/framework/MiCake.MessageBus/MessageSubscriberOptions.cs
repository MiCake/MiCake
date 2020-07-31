namespace MiCake.MessageBus
{
    /// <summary>
    /// A options for create a <see cref="IMessageSubscriber"/>.
    /// </summary>
    public class MessageSubscriberOptions
    {
        /// <summary>
        /// Name of the subscription.(for rabbit mq it's mean queue name.)
        /// </summary>
        public string SubscriptionName { get; set; }
    }
}
