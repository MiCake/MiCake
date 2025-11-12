using System;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Factory for creating MiCake interceptors using proper dependency injection patterns.
    /// Follows MiCake's lightweight and non-intrusive design principles.
    /// Avoids memory leaks by using the DI container correctly.
    /// </summary>
    internal sealed class MiCakeInterceptorFactory : IMiCakeInterceptorFactory
    {
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;
        private readonly ILogger<MiCakeEFCoreInterceptor> _logger;

        /// <summary>
        /// Initialize the factory with required dependencies
        /// </summary>
        /// <param name="saveChangesLifetime">The singleton save changes lifetime service</param>
        /// <param name="logger">Logger for the interceptor (optional)</param>
        public MiCakeInterceptorFactory(
            IEFSaveChangesLifetime saveChangesLifetime,
            ILogger<MiCakeEFCoreInterceptor> logger = null)
        {
            _saveChangesLifetime = saveChangesLifetime ?? throw new ArgumentNullException(nameof(saveChangesLifetime));
            _logger = logger ?? NullLogger<MiCakeEFCoreInterceptor>.Instance;
        }

        /// <summary>
        /// Create a new interceptor instance using the injected dependencies.
        /// </summary>
        /// <returns>MiCake EF Core interceptor (never null)</returns>
        public ISaveChangesInterceptor CreateInterceptor()
        {
            return new MiCakeEFCoreInterceptor(_saveChangesLifetime, _logger);
        }

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        public bool CanCreateInterceptor => _saveChangesLifetime != null;
    }

    /// <summary>
    /// Static helper for backward compatibility and easy access to interceptor creation.
    /// This class maintains the original API while using proper DI internally.
    /// Thread-safe implementation for multi-threaded scenarios.
    /// </summary>
    public static class MiCakeInterceptorFactoryHelper
    {
        private static volatile IMiCakeInterceptorFactory _factory;
        private static readonly Lock _lock = new Lock();

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
        internal static ISaveChangesInterceptor CreateInterceptor()
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
        public static void Reset()
        {
            lock (_lock)
            {
                _factory = null;
            }
        }
    }
}