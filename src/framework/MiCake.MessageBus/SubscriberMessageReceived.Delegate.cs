using MiCake.MessageBus.Messages;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Define the operation performed when a subscriber receives a message.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public delegate void SubscriberMessageReceived(object sender, Message message);
}
