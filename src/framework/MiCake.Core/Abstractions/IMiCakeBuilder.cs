using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// Configures MiCake services to be registered in the DI container
    /// </summary>
    public interface IMiCakeBuilder
    {
        /// <summary>
        /// Gets the service collection for registering additional services
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Completes the MiCake builder configuration.
        /// The MiCake system will be registered into the DI container.
        /// </summary>
        /// <returns>The builder for chaining</returns>
        IMiCakeBuilder Build();

        /// <summary>
        /// Gets current application options for the MiCake application.
        /// </summary>
        /// <returns></returns>
        MiCakeApplicationOptions GetApplicationOptions();
    }
}
