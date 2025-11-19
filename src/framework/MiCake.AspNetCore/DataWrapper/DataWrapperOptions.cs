using System.Collections.Generic;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// Configuration options for response data wrapping.
    /// </summary>
    public class DataWrapperOptions
    {
        /// <summary>
        /// Shows the stack trace information in error responses.
        /// Default: false
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
        public DataWrapperDefaultCode DefaultCodeSetting { get; set; } = new();

        /// <summary>
        /// Gets the wrapper factory, creating default if not configured.
        /// </summary>
        internal ResponseWrapperFactory GetOrCreateFactory()
        {
            return WrapperFactory ?? ResponseWrapperFactory.CreateDefault(this);
        }
    }

    /// <summary>
    /// Default business status codes for wrapped responses.
    /// </summary>
    public class DataWrapperDefaultCode
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
