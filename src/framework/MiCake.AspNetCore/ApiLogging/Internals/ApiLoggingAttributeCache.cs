using System.Reflection;
using MiCake.Util.Cache;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Cache for API logging attribute detection to improve performance.
    /// </summary>
    internal static class ApiLoggingAttributeCache
    {
        private static readonly BoundedLruCache<string, ApiLoggingAttributeInfo> _cache = new();

        /// <summary>
        /// Gets the cached attribute information for a controller action.
        /// </summary>
        /// <param name="actionDescriptor">The controller action descriptor</param>
        /// <returns>Cached attribute information</returns>
        public static ApiLoggingAttributeInfo GetInfo(ControllerActionDescriptor actionDescriptor)
        {
            var cacheKey = $"{actionDescriptor.ControllerTypeInfo.FullName}.{actionDescriptor.MethodInfo.Name}";

            return _cache.GetOrAdd(cacheKey, _ => BuildAttributeInfo(actionDescriptor));
        }

        private static ApiLoggingAttributeInfo BuildAttributeInfo(ControllerActionDescriptor actionDescriptor)
        {
            var methodInfo = actionDescriptor.MethodInfo;
            var controllerType = actionDescriptor.ControllerTypeInfo;

            // Check for SkipApiLogging - method level first, then controller level
            var skipLogging = methodInfo.GetCustomAttribute<SkipApiLoggingAttribute>() != null ||
                              controllerType.GetCustomAttribute<SkipApiLoggingAttribute>() != null;

            // Check for AlwaysLog - method level only
            var alwaysLog = methodInfo.GetCustomAttribute<AlwaysLogAttribute>() != null;

            // Check for LogFullResponse - method level only
            var logFullResponse = methodInfo.GetCustomAttribute<LogFullResponseAttribute>();

            return new ApiLoggingAttributeInfo
            {
                SkipLogging = skipLogging,
                AlwaysLog = alwaysLog,
                LogFullResponse = logFullResponse != null,
                MaxResponseSize = logFullResponse?.MaxSize ?? 0
            };
        }
    }

    /// <summary>
    /// Cached attribute information for an action.
    /// </summary>
    internal sealed class ApiLoggingAttributeInfo
    {
        /// <summary>
        /// Whether logging should be skipped for this action.
        /// </summary>
        public bool SkipLogging { get; set; }

        /// <summary>
        /// Whether this action should always be logged, ignoring status code exclusions.
        /// </summary>
        public bool AlwaysLog { get; set; }

        /// <summary>
        /// Whether to log the full response body.
        /// </summary>
        public bool LogFullResponse { get; set; }

        /// <summary>
        /// Maximum response size when LogFullResponse is true.
        /// 0 means no limit.
        /// </summary>
        public int MaxResponseSize { get; set; }
    }
}
