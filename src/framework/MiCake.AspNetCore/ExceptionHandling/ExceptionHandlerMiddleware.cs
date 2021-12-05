using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.AspNetCore.Internal;
using MiCake.Core;
using MiCake.Core.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// Although <see cref="ExceptionDataWrapperFilter"/>[<see cref="IAsyncExceptionFilter"/>] has been provided for action execution process, 
    /// Error information in other middleware scope needs to be intercepted
    /// </summary>
    internal class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DataWrapperOptions _wrapOptions;
        private readonly bool _useWrapper = false;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            IOptions<MiCakeAspNetOptions> options)
        {
            _next = next;
            _wrapOptions = options.Value?.DataWrapperOptions;

            _useWrapper = options.Value?.UseDataWrapper ?? false;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                //If has exception hander.
                await HandleException(context, ex);

                //If use data wrapper.
                await WrapperException(context, ex);
            }
        }

        private async Task HandleException(HttpContext context, Exception exception)
        {
            var currentRequestProvider = context.RequestServices;
            var miCakeCurrentRequest = currentRequestProvider.GetService<IMiCakeCurrentRequestContext>();

            if (miCakeCurrentRequest?.Handlers == null || miCakeCurrentRequest.Handlers.Count == 0)
                return;

            var _exceptionsHandlers = miCakeCurrentRequest.Handlers.Where(s => s.Handler is IMiCakeExceptionHandler).ToList();

            foreach (var handler in _exceptionsHandlers)
            {
                await (handler.Handler as IMiCakeExceptionHandler)?.Handle(new MiCakeExceptionContext(exception), context.RequestAborted);
            }
        }

        private async Task WrapperException(HttpContext context, Exception exception)
        {
            if (!_useWrapper)
                return;

            if (exception is SoftlyMiCakeException)
            {
                await WriteSoftExceptionResponse(context, exception as SoftlyMiCakeException);
            }
            else
            {
                await WriteExceptionResponse(context, exception);
            }
        }

        private async Task WriteSoftExceptionResponse(HttpContext context, SoftlyMiCakeException softMiCakeException)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status200OK;

            var wrapDataResult = new ApiResponse(message: softMiCakeException.Message, result: softMiCakeException.Details)
            {
                ErrorCode = softMiCakeException.Code,
                IsError = true
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition =  System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };
            var resultJsonData = JsonSerializer.Serialize(wrapDataResult, options);

            await httpResponse.WriteAsync(resultJsonData);
        }

        private async Task WriteExceptionResponse(HttpContext context, Exception exception)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status500InternalServerError;

            var micakeException = exception as MiCakeException;
            var stackTraceInfo = _wrapOptions.IsDebug ? exception.StackTrace : null;

            var wrapDataResult = new ApiError(exception.Message, micakeException?.Details, micakeException?.Code, stackTraceInfo);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };
            var resultJsonData = JsonSerializer.Serialize(wrapDataResult, options);

            await httpResponse.WriteAsync(resultJsonData);
        }
    }
}
