using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Filter to wrap exception responses.
    /// Handles both business exceptions and general exceptions.
    /// </summary>
    internal class ExceptionResponseWrapperFilter : IAsyncExceptionFilter
    {
        private readonly ResponseWrapperExecutor _executor;
        private readonly ILogger<ExceptionResponseWrapperFilter> _logger;

        public ExceptionResponseWrapperFilter(IOptions<MiCakeAspNetOptions> options, ILogger<ExceptionResponseWrapperFilter> logger)
        {
            var _options = options.Value?.DataWrapperOptions ?? new ResponseWrapperOptions();
            
            _executor = new ResponseWrapperExecutor(_options);
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return Task.CompletedTask;

            // Check if wrapping should be skipped
            if (ShouldSkipWrapping(context))
                return Task.CompletedTask;

            var exception = context.Exception;

            if (exception is IBusinessException businessException)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                context.HttpContext.SetBusinessExceptionContext(businessException);

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

            var statusCode = StatusCodes.Status500InternalServerError;
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
        /// Determines whether response wrapping should be skipped based on the SkipResponseWrapper attribute.
        /// </summary>
        private static bool ShouldSkipWrapping(FilterContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return SkipResponseWrapperCache.ShouldSkip(controllerActionDescriptor);
            }

            return false;
        }
    }
}
