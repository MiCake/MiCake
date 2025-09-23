namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Factory interface for creating MiCake EF Core interceptors.
    /// Follows proper dependency injection patterns to avoid memory leaks.
    /// </summary>
    internal interface IMiCakeInterceptorFactory
    {
        /// <summary>
        /// Create a new interceptor instance using the current service scope
        /// </summary>
        /// <returns>MiCake EF Core interceptor or null if service not available</returns>
        MiCakeEFCoreInterceptor CreateInterceptor();

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        bool CanCreateInterceptor { get; }
    }
}