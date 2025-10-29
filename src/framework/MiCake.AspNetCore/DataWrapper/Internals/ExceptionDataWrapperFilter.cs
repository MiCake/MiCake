using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Filter to wrap exception responses.
    /// Handles both MiCake exceptions and general exceptions.
    /// </summary>
    internal class ExceptionDataWrapperFilter : IAsyncExceptionFilter
    {
        private readonly ResponseWrapperExecutor _executor;
        private readonly DataWrapperOptions _options;

        public ExceptionDataWrapperFilter(IOptions<MiCakeAspNetOptions> options)
        {
            _options = options.Value?.DataWrapperOptions ?? new DataWrapperOptions();
            _executor = new ResponseWrapperExecutor(_options);
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return Task.CompletedTask;

            var exception = context.Exception;

            // Handle SlightException - treat as successful response with 200 status
            if (exception is ISlightException slightException)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                context.HttpContext.Items["MiCake.SlightException"] = exception;

                var wrappedData = _executor.WrapSuccess(
                    null,
                    context.HttpContext,
                    StatusCodes.Status200OK
                );

                context.ExceptionHandled = true;
                context.Result = new ObjectResult(wrappedData)
                {
                    StatusCode = StatusCodes.Status200OK
                };

                return Task.CompletedTask;
            }

            // Handle regular exceptions
            var statusCode = DetermineStatusCode(exception);
            var errorData = _executor.WrapError(
                exception,
                context.HttpContext,
                statusCode,
                context.Result
            );

            context.ExceptionHandled = true;
            context.Result = new ObjectResult(errorData)
            {
                StatusCode = statusCode
            };

            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines appropriate HTTP status code based on exception type.
        /// </summary>
        private static int DetermineStatusCode(System.Exception exception)
        {
            // MiCake exceptions default to 500 unless specified
            if (exception is MiCakeException)
                return StatusCodes.Status500InternalServerError;

            // Other exceptions default to 500
            return StatusCodes.Status500InternalServerError;
        }
    }
}
