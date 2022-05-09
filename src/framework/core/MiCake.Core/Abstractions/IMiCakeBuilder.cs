namespace MiCake.Core
{
    /// <summary>
    /// a builder for <see cref="IMiCakeApplication"/>
    /// </summary>
    public interface IMiCakeBuilder
    {
        /// <summary>
        /// Build an <see cref="IMiCakeApplication"/>
        /// </summary>
        /// <returns></returns>
        IMiCakeApplication Build();

        /// <summary>
        /// Adds a delegate for configuring micake application.
        /// It will execute before application initialize.
        /// </summary>
        /// <param name="configureApp">A delegate for configuring the <see cref="IMiCakeApplication"/>.</param>
        /// <returns>The <see cref="IMiCakeBuilder"/>.</returns>
        IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp);

        /// <summary>
        /// Adds a delegate for configuring micake application and services.
        /// It will execute before application initialize.
        /// </summary>
        /// <param name="configureApp">A delegate for configuring the <see cref="IMiCakeApplication"/> and <see cref="IServiceCollection"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp);
    }
}
