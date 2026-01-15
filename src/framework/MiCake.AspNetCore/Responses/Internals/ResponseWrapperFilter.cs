using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Filter to wrap successful HTTP responses.
    /// Only wraps ObjectResult to ensure proper JSON serialization.
    /// </summary>
    internal class ResponseWrapperFilter : IAsyncResultFilter
    {
        private readonly ResponseWrapperExecutor _executor;
        private readonly ResponseWrapperOptions _options;

        public ResponseWrapperFilter(IOptions<MiCakeAspNetOptions> options)
        {
            _options = options.Value?.DataWrapperOptions ?? new ResponseWrapperOptions();
            _executor = new ResponseWrapperExecutor(_options);
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Check if wrapping should be skipped
            if (ShouldSkipWrapping(context))
            {
                await next();
                return;
            }

            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;

                if (!_options.IgnoreStatusCodes.Contains(statusCode))
                {
                    var wrappedData = _executor.WrapSuccess(
                        objectResult?.Value,
                        context.HttpContext,
                        statusCode
                    );

                    objectResult?.Value = wrappedData;
                    objectResult?.DeclaredType = wrappedData?.GetType();
                }
            }

            await next();
        }

        /// <summary>
        /// Determines whether response wrapping should be skipped based on the SkipResponseWrapper attribute.
        /// </summary>
        private static bool ShouldSkipWrapping(FilterContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return SkipResponseWrapperCache.ShouldSkip(controllerActionDescriptor);
            }

            return false;
        }
    }
}
