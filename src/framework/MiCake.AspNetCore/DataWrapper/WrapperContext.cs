using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// Context information passed to wrapper factory delegates.
    /// </summary>
    public class WrapperContext
    {
        /// <summary>
        /// The HTTP context for the current request.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// The original data to be wrapped.
        /// The value may be null.
        /// </summary>
        public object OriginalData { get; }

        public WrapperContext(HttpContext httpContext, int statusCode, object originalData)
        {
            HttpContext = httpContext;
            StatusCode = statusCode;
            OriginalData = originalData;
        }
    }

    /// <summary>
    /// Context information for error wrapper factory.
    /// </summary>
    public class ErrorWrapperContext : WrapperContext
    {
        /// <summary>
        /// The exception that occurred (if any).
        /// </summary>
        public System.Exception Exception { get; }

        public ErrorWrapperContext(HttpContext httpContext, int statusCode, object originalData, System.Exception exception)
            : base(httpContext, statusCode, originalData)
        {
            Exception = exception;
        }
    }
}
