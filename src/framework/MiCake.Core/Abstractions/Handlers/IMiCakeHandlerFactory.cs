using System;

namespace MiCake.Core.Handlers
{
    /// <summary>
    /// An interface for handler which can create an instance of an executable handler.
    /// </summary>
    public interface IMiCakeHandlerFactory : IMiCakeHandler
    {
        /// <summary>
        /// Creates an instance of the micake handler.
        /// </summary>
        /// <param name="serviceProvider">The request <see cref="IServiceProvider"/>.</param>
        /// <returns>An instance of the executable filter.</returns>
        IMiCakeHandler CreateInstance(IServiceProvider serviceProvider);
    }
}
