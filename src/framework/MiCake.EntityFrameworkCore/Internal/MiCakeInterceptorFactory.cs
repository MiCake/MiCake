using System;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Factory for creating MiCake interceptors using proper dependency injection patterns.
    /// </summary>
    internal sealed class MiCakeInterceptorFactory : IMiCakeInterceptorFactory
    {
        private readonly ILogger<MiCakeEFCoreInterceptor> _logger;
        private readonly ILogger<MiCakeDbCommandInterceptor> _commandLogger;

        /// <summary>
        /// Initialize the factory with required dependencies
        /// </summary>
        /// <param name="logger">Logger for the save interceptor (optional)</param>
        /// <param name="commandLogger">Logger for the command interceptor (optional)</param>
        public MiCakeInterceptorFactory(
            ILogger<MiCakeEFCoreInterceptor>? logger = null,
            ILogger<MiCakeDbCommandInterceptor>? commandLogger = null)
        {
            _logger = logger ?? NullLogger<MiCakeEFCoreInterceptor>.Instance;
            _commandLogger = commandLogger ?? NullLogger<MiCakeDbCommandInterceptor>.Instance;
        }

        /// <summary>
        /// Create a new interceptor instance using the injected dependencies.
        /// </summary>
        /// <returns>MiCake EF Core interceptor (never null)</returns>
        public ISaveChangesInterceptor CreateInterceptor(IServiceProvider? serviceProvider = null)
        {
            return new MiCakeEFCoreInterceptor(_logger, serviceProvider);
        }

        /// <summary>
        /// Create the write-guard command interceptor.
        /// </summary>
        /// <returns>The command interceptor instance (never null).</returns>
        public IDbCommandInterceptor CreateCommandInterceptor(IServiceProvider? serviceProvider = null)
        {
            return new MiCakeDbCommandInterceptor(_commandLogger, serviceProvider);
        }

        /// <summary>
        /// Check if the factory can create interceptors
        /// </summary>
        public bool CanCreateInterceptor => true;
    }

    /// <summary>
    /// Static helper for easy access to interceptor creation when configuring DbContextOptionsBuilder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper creates an <see cref="IMiCakeInterceptorFactory"/> instance from the DI container during application initialization
    /// and stores it statically for later use when configuring DbContextOptionsBuilder.
    /// It works well in single-startup scenarios like ASP.NET Core, but caution is advised in cross-application or cross-test lifetime scenarios.
    /// </para>
    /// <para>
    /// This helper exists for backwards compatibility and convenience, but using the
    /// static API can cause cross-test or cross-application lifetime issues because it
    /// stores a reference to an <see cref="IMiCakeInterceptorFactory"/> instance which is
    /// created from the DI container.
    /// </para>
    /// <para>
    /// Preferred approach: use dependency injection (DI) to obtain an
    /// <see cref="IMiCakeInterceptorFactory"/> instance (or register the factory as a
    /// singleton) and call <c>options.UseMiCakeInterceptors(sp)</c> from
    /// <c>AddDbContext((sp, options) => ...)</c>. That is DI-first and avoids relying
    /// on a global statically stored factory. The DI approach prevents holding onto a
    /// disposed service provider across test or application boundaries.
    /// </para>
    /// </remarks>
    public static class MiCakeInterceptorFactoryHelper
    {
        private static volatile IMiCakeInterceptorFactory? _factory;
        private static readonly Lock _lock = new();

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
        internal static ISaveChangesInterceptor? CreateInterceptor(IServiceProvider? serviceProvider = null)
        {
            var factory = _factory;
            try
            {
                return factory?.CreateInterceptor(serviceProvider);
            }
            catch (ObjectDisposedException)
            {
                // If the underlying service provider was disposed, clear the reference
                // to avoid throwing in future calls and let callers fallback to DI.
                Reset();
                return null;
            }
        }

        /// <summary>
        /// Create the write-guard command interceptor using the configured factory.
        /// </summary>
        /// <returns>The command interceptor, or null when no factory is configured.</returns>
        internal static IDbCommandInterceptor? CreateCommandInterceptor(IServiceProvider? serviceProvider = null)
        {
            var factory = _factory;
            try
            {
                return factory?.CreateCommandInterceptor(serviceProvider);
            }
            catch (ObjectDisposedException)
            {
                Reset();
                return null;
            }
        }

        /// <summary>
        /// Check if the factory is properly configured
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                try
                {
                    var factory = _factory;
                    return factory?.CanCreateInterceptor == true;
                }
                catch (ObjectDisposedException)
                {
                    Reset();
                    return false;
                }
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