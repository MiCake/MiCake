using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

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
        /// <param name="serviceProvider">The provider of the scope that owns the DbContext,
        /// used by the interceptor to resolve the write pipeline at runtime.</param>
        /// <returns>MiCake EF Core interceptor or null if service not available</returns>
        ISaveChangesInterceptor CreateInterceptor(IServiceProvider? serviceProvider = null);

        /// <summary>
        /// Create the write-guard command interceptor for the DbContext options.
        /// </summary>
        /// <param name="serviceProvider">The provider of the scope that owns the DbContext,
        /// used by the interceptor to resolve the write pipeline at runtime.</param>
        /// <returns>The command interceptor instance.</returns>
        IDbCommandInterceptor CreateCommandInterceptor(IServiceProvider? serviceProvider = null);

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        bool CanCreateInterceptor { get; }
    }
}