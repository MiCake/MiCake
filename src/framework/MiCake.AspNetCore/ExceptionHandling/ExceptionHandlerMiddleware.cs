using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.AspNetCore.Internal;
using MiCake.Core;
using MiCake.Core.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// Middleware for handling exceptions that occur outside the MVC action execution pipeline.
    /// Although ExceptionDataWrapperFilter (IAsyncExceptionFilter) handles exceptions in action execution, 
    /// error information in other middleware scopes needs to be intercepted.
    /// </summary>
    internal class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DataWrapperOptions _wrapOptions;
        private readonly ResponseWrapperExecutor _wrapperExecutor;
        private readonly bool _useWrapper = false;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            IOptions<MiCakeAspNetOptions> options)
        {
            _next = next;
            _wrapOptions = options.Value?.DataWrapperOptions ?? new DataWrapperOptions();
            _wrapperExecutor = new ResponseWrapperExecutor(_wrapOptions);

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

        private static async Task HandleException(HttpContext context, Exception exception)
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

            if (exception is SlightMiCakeException)
            {
                await WriteSlightExceptionResponse(context, exception as SlightMiCakeException, GetOptions());
            }
            else
            {
                await WriteExceptionResponse(context, exception);
            }
        }

        private static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
        }

        private async Task WriteSlightExceptionResponse(HttpContext context, SlightMiCakeException slightMiCakeException, JsonSerializerOptions options)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status200OK;

            // Store the slight exception for ResponseWrapperExecutor to detect
            context.SetSlightException(slightMiCakeException);

            // Use the factory to wrap the response consistently
            var wrappedData = _wrapperExecutor.WrapSuccess(
                slightMiCakeException.Details,
                context,
                StatusCodes.Status200OK
            );

            var resultJsonData = JsonSerializer.Serialize(wrappedData, options);

            await httpResponse.WriteAsync(resultJsonData);
        }

        private async Task WriteExceptionResponse(HttpContext context, Exception exception)
        {
            var httpResponse = context.Response;
            httpResponse.StatusCode = StatusCodes.Status500InternalServerError;

            // Use the factory to wrap the response consistently
            var wrappedData = _wrapperExecutor.WrapError(
                exception,
                context,
                StatusCodes.Status500InternalServerError
            );

            var resultJsonData = JsonSerializer.Serialize(wrappedData, GetOptions());

            await httpResponse.WriteAsync(resultJsonData);
        }
    }
}
