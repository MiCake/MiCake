using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    internal class ExceptionDataWrapper : IAsyncExceptionFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;
        private readonly DataWrapperOptions _options;

        public ExceptionDataWrapper(
            IOptions<MiCakeAspNetOptions> options,
            IDataWrapperExecutor wrapperExecutor)
        {
            _wrapperExecutor = wrapperExecutor;
            _options = options.Value?.DataWrapperOptions;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return Task.CompletedTask;

            if (!_options.NoWrapStatusCode.Any(s => s == context.HttpContext.Response.StatusCode))
            {
                var wrapContext = new DataWrapperContext(context.Result,
                                                          context.HttpContext,
                                                          _options,
                                                          context.ActionDescriptor);

                var wrapedData = _wrapperExecutor.WrapFailedResult(context.Result, context.Exception, wrapContext);
                if (!(wrapedData is ApiError))
                    context.ExceptionHandled = true;

                context.Result = new ObjectResult(wrapedData);
            }

            return Task.CompletedTask;
        }
    }
}
