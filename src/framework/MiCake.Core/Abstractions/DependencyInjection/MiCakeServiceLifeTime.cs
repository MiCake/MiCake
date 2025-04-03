namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service
    /// </summary>
    public enum MiCakeServiceLifetime
    {
        /// <summary>
        /// The service is created once per application and shared throughout the application.
        /// </summary>
        Singleton = 0,

        /// <summary>
        /// The service is created once per request (connection).
        /// /// </summary>
        Scoped = 1,

        /// <summary>
        /// The service is created each time it is requested.
        /// /// </summary>
        Transient = 2
    }
}
