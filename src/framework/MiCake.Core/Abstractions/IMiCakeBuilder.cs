using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// Configures MiCake services to be registered in the DI container
    /// </summary>
    public interface IMiCakeBuilder
    {
        /// <summary>
        /// Completes the MiCake builder configuration.
        /// The actual IMiCakeApplication will be resolved from the DI container when needed.
        /// </summary>
        /// <returns>The builder for chaining</returns>
        IMiCakeBuilder Build();

        /// <summary>
        /// Adds a delegate for configuring MiCake application.
        /// Configuration will be applied when the application is resolved from DI container.
        /// </summary>
        /// <param name="configureApp">A delegate for configuring the <see cref="IMiCakeApplication"/>.</param>
        /// <returns>The <see cref="IMiCakeBuilder"/>.</returns>
        IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp);

        /// <summary>
        /// Adds a delegate for configuring MiCake application and services.
        /// Configuration will be applied when the application is resolved from DI container.
        /// </summary>
        /// <param name="configureApp">A delegate for configuring the <see cref="IMiCakeApplication"/> and <see cref="IServiceCollection"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp);
    }
}
