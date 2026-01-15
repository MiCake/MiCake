using MiCake.Core;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Extension methods for HttpContext to manage BusinessException handling.
    /// Provides a centralized, consistent way to store and retrieve BusinessException instances.
    /// </summary>
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Key used to store BusinessException in HttpContext.Items dictionary.
        /// </summary>
        private const string BusinessExceptionKey = "MiCake.BusinessException";

        /// <summary>
        /// Stores a BusinessException in the HttpContext for later retrieval.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="exception">The BusinessException to store.</param>
        public static void SetBusinessExceptionContext(this HttpContext httpContext, IBusinessException exception)
        {
            if (httpContext?.Items != null)
            {
                httpContext.Items[BusinessExceptionKey] = exception;
            }
        }

        /// <summary>
        /// Attempts to retrieve a previously stored BusinessException from the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="exception">The BusinessException if found; otherwise null.</param>
        /// <returns>True if a BusinessException was found; otherwise false.</returns>
        public static bool TryGetBusinessException(this HttpContext httpContext, out IBusinessException? exception)
        {
            exception = null;

            if (httpContext?.Items == null)
                return false;

            if (httpContext.Items.TryGetValue(BusinessExceptionKey, out var item) &&
                item is IBusinessException slightEx)
            {
                exception = slightEx;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a BusinessException has been stored in the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>True if a BusinessException is stored; otherwise false.</returns>
        public static bool HasBusinessException(this HttpContext httpContext)
        {
            return TryGetBusinessException(httpContext, out _);
        }

        /// <summary>
        /// Retrieves a stored BusinessException from the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>The stored BusinessException, or null if not found.</returns>
        public static IBusinessException? GetBusinessException(this HttpContext httpContext)
        {
            TryGetBusinessException(httpContext, out var exception);
            return exception;
        }
    }
}
