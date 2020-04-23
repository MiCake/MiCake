using MiCake.Core.Modularity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        private readonly MiCakeModuleCollection _miCakeModules;

        public DataWrapperFilter()
        {
            _wrapperExecutor = new DefaultWrapperExecutor();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var statusCode = context.HttpContext.Response.StatusCode;

            if (context.Result is ObjectResult objectResult && !_options.NoWrapStatusCode.Any(s => s == statusCode))
            {
                var wrappContext = new DataWrapperContext(context.Result,
                                                          context.HttpContext,
                                                          _options,
                                                          _miCakeModules,
                                                          context.ActionDescriptor);

                var wrappedData = _wrapperExecutor.WrapSuccesfullysResult(context.Result, wrappContext);
                objectResult.Value = wrappedData;
            }

            await next();
        }
    }
}
