using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Filter to wrap successful HTTP responses.
    /// Only wraps ObjectResult to ensure proper JSON serialization.
    /// </summary>
    internal class DataWrapperFilter : IAsyncResultFilter
    {
        private readonly ResponseWrapperExecutor _executor;
        private readonly DataWrapperOptions _options;

        public DataWrapperFilter(IOptions<MiCakeAspNetOptions> options)
        {
            _options = options.Value?.DataWrapperOptions ?? new DataWrapperOptions();
            _executor = new ResponseWrapperExecutor(_options);
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;

                // Skip wrapping for configured status codes
                if (!_options.IgnoreStatusCodes.Any(s => s == statusCode))
                {
                    var wrappedData = _executor.WrapSuccess(
                        objectResult.Value,
                        context.HttpContext,
                        statusCode
                    );

                    // Update ObjectResult to use wrapped data
                    // This ensures ASP.NET Core's JSON serialization is applied
                    objectResult.Value = wrappedData;
                    objectResult.DeclaredType = wrappedData?.GetType();
                }
            }

            await next();
        }
    }
}
