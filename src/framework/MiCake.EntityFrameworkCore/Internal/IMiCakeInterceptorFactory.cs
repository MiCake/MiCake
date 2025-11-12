using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Factory interface for creating MiCake EF Core interceptors.
    /// </summary>
    public interface IMiCakeInterceptorFactory
    {
        /// <summary>
        /// Create a new interceptor instance using the current service scope
        /// </summary>
        /// <returns>MiCake EF Core interceptor or null if service not available</returns>
        ISaveChangesInterceptor CreateInterceptor();

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        bool CanCreateInterceptor { get; }
    }
}