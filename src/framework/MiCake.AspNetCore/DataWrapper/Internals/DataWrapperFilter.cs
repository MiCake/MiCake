using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Wrap current http response data.
    /// Only <see cref="ObjectResult"/> will be wrapped.
    /// </summary>
    internal class DataWrapperFilter : IAsyncResultFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;
        private readonly DataWrapperOptions _options;

        public DataWrapperFilter(
            IOptions<MiCakeAspNetOptions> options,
            IDataWrapperExecutor wrapperExecutor)
        {
            _wrapperExecutor = wrapperExecutor;
            _options = options.Value?.DataWrapperOptions;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;

                if (!_options.NoWrapStatusCode.Any(s => s == statusCode))
                {
                    var wrappContext = new DataWrapperContext(context.Result,
                                                              context.HttpContext,
                                                              _options,
                                                              context.ActionDescriptor);

                    var wrappedData = _wrapperExecutor.WrapSuccesfullysResult(objectResult.Value, wrappContext);
                    objectResult.Value = wrappedData;
                    objectResult.DeclaredType = wrappedData.GetType();
                }
            }

            await next();
        }
    }
}
