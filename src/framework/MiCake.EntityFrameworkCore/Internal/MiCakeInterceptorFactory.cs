using System;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Factory for creating MiCake interceptors using proper dependency injection patterns.
    /// Follows MiCake's lightweight and non-intrusive design principles.
    /// Avoids memory leaks by using the DI container correctly.
    /// </summary>
    internal sealed class MiCakeInterceptorFactory : IMiCakeInterceptorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initialize the factory with a service provider
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving dependencies</param>
        public MiCakeInterceptorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Create a new interceptor instance using the singleton IEFSaveChangesLifetime service.
        /// The service handles scoped dependency resolution internally.
        /// </summary>
        /// <returns>MiCake EF Core interceptor (never null)</returns>
        public MiCakeEFCoreInterceptor CreateInterceptor()
        {
            // Resolve the singleton IEFSaveChangesLifetime service 
            // This service uses lazy resolution for its scoped dependencies
            var saveChangesLifetime = _serviceProvider.GetRequiredService<IEFSaveChangesLifetime>();
            return new MiCakeEFCoreInterceptor(saveChangesLifetime);
        }

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        public bool CanCreateInterceptor => _serviceProvider != null;
    }

    /// <summary>
    /// Static helper for backward compatibility and easy access to interceptor creation.
    /// This class maintains the original API while using proper DI internally.
    /// Thread-safe implementation for multi-threaded scenarios.
    /// </summary>
    internal static class MiCakeInterceptorFactoryHelper
    {
        private static volatile IMiCakeInterceptorFactory _factory;
        private static readonly object _lock = new object();

        /// <summary>
        /// Configure the factory instance (called during DI setup)
        /// </summary>
        /// <param name="factory">The factory instance to use</param>
        internal static void Configure(IMiCakeInterceptorFactory factory)
        {
            lock (_lock)
            {
                _factory = factory;
            }
        }

        /// <summary>
        /// Create a new interceptor using the configured factory
        /// </summary>
        /// <returns>MiCake EF Core interceptor (never null if factory is configured)</returns>
        internal static MiCakeEFCoreInterceptor CreateInterceptor()
        {
            var factory = _factory; // Read volatile field once
            return factory?.CreateInterceptor();
        }

        /// <summary>
        /// Check if the factory is properly configured
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                var factory = _factory; // Read volatile field once
                return factory?.CanCreateInterceptor == true;
            }
        }

        /// <summary>
        /// Reset the factory configuration (used in testing scenarios)
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _factory = null;
            }
        }
    }
}