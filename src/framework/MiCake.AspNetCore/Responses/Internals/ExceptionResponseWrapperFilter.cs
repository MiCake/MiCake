using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Filter to wrap exception responses.
    /// Handles both MiCake exceptions and general exceptions.
    /// </summary>
    internal class ExceptionResponseWrapperFilter : IAsyncExceptionFilter
    {
        private readonly ResponseWrapperExecutor _executor;
        private readonly ResponseWrapperOptions _options;
        private readonly ILogger<ExceptionResponseWrapperFilter> _logger;

        public ExceptionResponseWrapperFilter(IOptions<MiCakeAspNetOptions> options, ILogger<ExceptionResponseWrapperFilter> logger)
        {
            _options = options.Value?.DataWrapperOptions ?? new ResponseWrapperOptions();
            _executor = new ResponseWrapperExecutor(_options);
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return Task.CompletedTask;

            var exception = context.Exception;

            if (exception is ISlightException slightException)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                context.HttpContext.SetSlightException(slightException);

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

            _logger.LogError(exception, "An unhandled exception occurred.");

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
            if (exception is MiCakeException)
                return StatusCodes.Status500InternalServerError;

            return StatusCodes.Status500InternalServerError;
        }
    }
}
