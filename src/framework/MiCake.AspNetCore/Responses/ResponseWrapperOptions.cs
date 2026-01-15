using System;
using System.Collections.Generic;
using System.Threading;

namespace MiCake.AspNetCore.Responses
{
    /// <summary>
    /// Configuration options for response data wrapping.
    /// </summary>
    public class ResponseWrapperOptions
    {
        /// <summary>
        /// Shows the stack trace information in error responses.
        /// Default: false
        /// 
        /// <para>
        /// The option is typically enabled in development environments to aid debugging.
        /// In production, it is advisable to keep this disabled to avoid exposing sensitive information.
        /// </para>
        /// </summary>
        public bool ShowStackTraceWhenError { get; set; } = false;

        /// <summary>
        /// HTTP status codes that should not be wrapped.
        /// Default: 201, 202, 404
        /// </summary>
        public List<int> IgnoreStatusCodes { get; set; } = [201, 202, 404];

        /// <summary>
        /// Whether to wrap ProblemDetails responses.
        /// When false, ProblemDetails maintains its standard ASP.NET Core format.
        /// Default: true
        /// </summary>
        public bool WrapProblemDetails { get; set; } = true;

        /// <summary>
        /// Custom factory for creating response wrappers.
        /// If not set, uses the default StandardResponse and ErrorResponse models.
        /// </summary>
        public ResponseWrapperFactory? WrapperFactory { get; set; }

        /// <summary>
        /// Default status codes used in wrapped responses.
        /// </summary>
        public ResponseWrapperDefaultCodes DefaultCodeSetting { get; set; } = new();

        /// <summary>
        /// Lazy-initialized wrapper factory for thread-safe, single-instance caching.
        /// Ensures the factory is created only once, even in multi-threaded scenarios.
        /// </summary>
        private readonly Lazy<ResponseWrapperFactory> _lazyFactory;

        public ResponseWrapperOptions()
        {
            _lazyFactory = new Lazy<ResponseWrapperFactory>(
               () => ResponseWrapperFactory.CreateDefault(this),
               LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the wrapper factory, creating and caching the default if not configured.
        /// </summary>
        /// <remarks>
        /// If a custom WrapperFactory is set, it is returned directly without lazy initialization.
        /// This allows for runtime factory changes while still providing caching for the default factory.
        /// </remarks>
        internal ResponseWrapperFactory GetOrCreateFactory()
        {
            // If custom factory is set, return it directly (prioritize explicit configuration)
            if (WrapperFactory != null)
                return WrapperFactory;

            return _lazyFactory.Value;
        }
    }

    /// <summary>
    /// Default business status codes for wrapped responses.
    /// </summary>
    public class ResponseWrapperDefaultCodes
    {
        /// <summary>
        /// Code returned for successful operations.
        /// Default: "0"
        /// </summary>
        public string Success { get; set; } = "0";

        /// <summary>
        /// Code returned for ProblemDetails responses.
        /// Default: "9998"
        /// </summary>
        public string ProblemDetails { get; set; } = "9998";

        /// <summary>
        /// Code returned for error responses.
        /// Default: "9999"
        /// </summary>
        public string Error { get; set; } = "9999";
    }
}
