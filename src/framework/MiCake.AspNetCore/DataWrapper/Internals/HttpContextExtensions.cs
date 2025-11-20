using MiCake.Core;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Extension methods for HttpContext to manage SlightException handling.
    /// Provides a centralized, consistent way to store and retrieve SlightException instances.
    /// </summary>
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Key used to store SlightException in HttpContext.Items dictionary.
        /// </summary>
        private const string SlightExceptionKey = "MiCake.SlightException";

        /// <summary>
        /// Stores a SlightException in the HttpContext for later retrieval.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="exception">The SlightException to store.</param>
        public static void SetSlightException(this HttpContext httpContext, ISlightException exception)
        {
            if (httpContext?.Items != null)
            {
                httpContext.Items[SlightExceptionKey] = exception;
            }
        }

        /// <summary>
        /// Attempts to retrieve a previously stored SlightException from the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="exception">The SlightException if found; otherwise null.</param>
        /// <returns>True if a SlightException was found; otherwise false.</returns>
        public static bool TryGetSlightException(this HttpContext httpContext, out ISlightException? exception)
        {
            exception = null;

            if (httpContext?.Items == null)
                return false;

            if (httpContext.Items.TryGetValue(SlightExceptionKey, out var item) &&
                item is ISlightException slightEx)
            {
                exception = slightEx;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a SlightException has been stored in the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>True if a SlightException is stored; otherwise false.</returns>
        public static bool HasSlightException(this HttpContext httpContext)
        {
            return TryGetSlightException(httpContext, out _);
        }

        /// <summary>
        /// Retrieves a stored SlightException from the HttpContext.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>The stored SlightException, or null if not found.</returns>
        public static ISlightException? GetSlightException(this HttpContext httpContext)
        {
            TryGetSlightException(httpContext, out var exception);
            return exception;
        }
    }
}
