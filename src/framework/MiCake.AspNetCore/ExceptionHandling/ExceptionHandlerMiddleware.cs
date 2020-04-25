using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// Although <see cref="ExceptionDataWrapper"/>[<see cref="IAsyncExceptionFilter"/>] has been provided for action execution process, 
    /// Error information in other middleware scope needs to be intercepted
    /// </summary>
    internal class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private DataWrapperOptions _wrapOptions;

        public ExceptionHandlerMiddleware(RequestDelegate next, IOptions<MiCakeAspNetOptions> options)
        {
            _next = next;
            _wrapOptions = options.Value?.DataWrapperOptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex) when (ex is SoftMiCakeException)
            {
                await WriteSoftExceptionResponse(context, ex as SoftMiCakeException);
            }
            catch (Exception ex)
            {
                await WriteExceptionResponse(context, ex);
            }
        }

        private async Task WriteSoftExceptionResponse(HttpContext context, SoftMiCakeException softMiCakeException)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status200OK;

            var wrapDataResult = new ApiResponse(message: softMiCakeException.Message,
                                                 errorCode: softMiCakeException.Code,
                                                 result: softMiCakeException.Details,
                                                 statusCode: 200);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true,
            };
            var resultJsonData = JsonSerializer.Serialize(wrapDataResult, options);

            await httpResponse.WriteAsync(resultJsonData);
        }

        private async Task WriteExceptionResponse(HttpContext context, Exception exception)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status500InternalServerError;

            var micakeException = exception as MiCakeException;
            var stackTraceInfo = _wrapOptions.IsDebug ? exception.StackTrace : string.Empty;

            var wrapDataResult = new ApiError(exception.Message, micakeException?.Details, micakeException?.Code, stackTraceInfo);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true,
            };
            var resultJsonData = JsonSerializer.Serialize(wrapDataResult, options);

            await httpResponse.WriteAsync(resultJsonData);
        }
    }
}
