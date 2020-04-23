using MiCake.Core.Modularity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    internal class ExceptionDataWrapper : IAsyncExceptionFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;
        private readonly DataWrapperOptions _options;
        private readonly MiCakeModuleCollection _miCakeModules;

        public ExceptionDataWrapper()
        {
            _wrapperExecutor = new DefaultWrapperExecutor();
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return Task.CompletedTask;


            var wrappContext = new DataWrapperContext(context.Result,
                                                      context.HttpContext,
                                                      _options,
                                                      _miCakeModules,
                                                      context.ActionDescriptor);

            var wrappedData = _wrapperExecutor.WrapFailedResult(context.Result, context.Exception, wrappContext);
            if (!(wrappedData is ApiError))
                context.ExceptionHandled = true;

            context.Result = new ObjectResult(wrappedData);

            return Task.CompletedTask;
        }
    }
}
