using MiCake.Core;
using MiCake.Core.Util;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Default implementation for <see cref="IDataWrapperExecutor"/>
    /// </summary>
    internal class DefaultWrapperExecutor : IDataWrapperExecutor
    {
        public DefaultWrapperExecutor()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual object WrapPureException(PureException exception, DataWrapperContext wrapperContext)
        {
            var httpContext = wrapperContext.HttpContext;

            CheckValue.NotNull(httpContext, nameof(HttpContext));

            //Given Ok Code for this exception.
            httpContext.Response.StatusCode = StatusCodes.Status200OK;

            return ApiResponse.Failure(exception.Message, exception.Code);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual object WrapApiResponse(object orignalData, DataWrapperContext wrapperContext)
        {
            CheckValue.NotNull(wrapperContext, nameof(wrapperContext));

            if (orignalData is IWrappedResponse)
                return orignalData;

            var data = ApiResponse.Success(orignalData);
            data.Message = ResponseMessage.Success;

            return data;
        }
    }
}
