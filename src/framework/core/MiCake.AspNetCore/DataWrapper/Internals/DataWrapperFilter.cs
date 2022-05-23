using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Wrap current http response data.
    /// Only <see cref="ObjectResult"/> will be wrapped.
    /// </summary>
    internal class DataWrapperFilter : IAsyncResultFilter
    {
        private readonly IDataWrapperExecutor _wrapperExecutor;

        public DataWrapperFilter(IDataWrapperExecutor wrapperExecutor)
        {
            _wrapperExecutor = wrapperExecutor;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is OkObjectResult objectResult)
            {
                var wrappContext = new DataWrapperContext(context.Result,
                                                          context.HttpContext,
                                                          context.ActionDescriptor);

                var wrappedData = _wrapperExecutor.WrapApiResponse(objectResult.Value!, wrappContext);
                objectResult.Value = wrappedData;
                objectResult.DeclaredType = wrappedData.GetType();
            }

            await next();
        }
    }
}
