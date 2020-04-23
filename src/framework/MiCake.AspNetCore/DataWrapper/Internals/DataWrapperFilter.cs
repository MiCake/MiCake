using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Wrap current http response data.
    /// Only <see cref="ObjectResult"/> will be wrapped.
    /// </summary>
    internal class DataWrapperFilter : IAsyncResultFilter
    {
        private readonly IDataWrapperExecutor wrapperExecutor;

        public DataWrapperFilter()
        {
            wrapperExecutor = new DefaultWrapperExecutor();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var wrappedData = wrapperExecutor.WrapSuccesfullysResult(context.Result, null);
                objectResult.Value = wrappedData;
            }

            await next();
        }
    }
}
