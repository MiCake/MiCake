using MiCake.MessageBus.Transport;

namespace MiCake.MessageBus.Helpers
{
    public static class TransportExtensions
    {
        /// <summary>
        /// Indicate current <see cref="ITransport"/> is closed.
        /// It's mean no connection or connection closed.
        /// </summary>
        /// <param name="transport"></param>
        /// <returns></returns>
        public static bool IsClose(this ITransport transport)
            => transport.Connection == null || transport.Connection.IsClosed;
    }
}
