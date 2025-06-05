using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    internal class ExceptionDataWrapperFilter : IAsyncExceptionFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;
        private readonly DataWrapperOptions _options;

        public ExceptionDataWrapperFilter(
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

            //httpContext status code is always be 0.

            var wrapContext = new DataWrapperContext(context.Result,
                                                     context.HttpContext,
                                                     _options,
                                                     context.ActionDescriptor);

            var wrappedData = _wrapperExecutor.WrapFailedResult(context.Result, context.Exception, wrapContext);
            if (wrappedData is not ApiError)
            {
                context.ExceptionHandled = true;
                context.Result = new ObjectResult(wrappedData);
            }

            //exceptionContext.Result != null || exceptionContext.Exception == null || exceptionContext.ExceptionHandled
            //Therefore, the exceptionContext.Result is not assigned here and is handled by middleware

            return Task.CompletedTask;
        }
    }
}
