using MiCake.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    internal class ExceptionDataWrapperFilter : IAsyncExceptionFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;

        public ExceptionDataWrapperFilter(IDataWrapperExecutor wrapperExecutor)
        {
            _wrapperExecutor = wrapperExecutor;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled || context.Exception is not PureException)
                return Task.CompletedTask;

            var wrapContext = new DataWrapperContext(context.Result!,
                                                     context.HttpContext,
                                                     context.ActionDescriptor);

            var wrappedData = _wrapperExecutor.WrapPureException((context.Exception as PureException)!, wrapContext);

            context.ExceptionHandled = true;
            context.Result = new ObjectResult(wrappedData);

            return Task.CompletedTask;
        }
    }
}
