using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Simplified executor for wrapping responses.
    /// Focuses solely on wrapping logic using factory pattern.
    /// </summary>
    internal class ResponseWrapperExecutor(ResponseWrapperOptions options)
    {
        private readonly ResponseWrapperOptions _options = options ?? throw new ArgumentNullException(nameof(options));

        /// <summary>
        /// Wraps a successful response using the configured factory.
        /// </summary>
        public object? WrapSuccess(object? originalData, HttpContext httpContext, int statusCode)
        {
            if (originalData is IResponseWrapper)
                return originalData;

            if (!_options.WrapProblemDetails && IsProblemDetails(originalData))
                return originalData;

            var context = new WrapperContext(httpContext, statusCode, originalData);
            if (httpContext.TryGetSlightException(out var slightException))
            {
                var slightExceptionData = new SlightExceptionData
                {
                    Code = slightException!.Code,
                    Message = slightException.Message,
                    Details = slightException.Details
                };
                context = new WrapperContext(httpContext, statusCode, slightExceptionData);
            }

            return _options.GetOrCreateFactory()?.SuccessFactory?.Invoke(context);
        }

        /// <summary>
        /// Wraps an error response using the configured factory.
        /// </summary>
        public object? WrapError(Exception exception, HttpContext httpContext, int statusCode, object? originalData = null)
        {
            var context = new ErrorWrapperContext(httpContext, statusCode, originalData, exception);
            return _options.GetOrCreateFactory()?.ErrorFactory?.Invoke(context);
        }

        private static bool IsProblemDetails(object? data)
        {
            return data is ProblemDetails || data is HttpValidationProblemDetails || data is ValidationProblemDetails;
        }
    }

    /// <summary>
    /// Wrapper data for SlightException to be passed to factory.
    /// Allows factory to customize response format while preserving exception details.
    /// </summary>
    internal class SlightExceptionData
    {
        /// <summary>
        /// Business operation status code from the exception.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Exception message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Additional details from the exception.
        /// </summary>
        public object? Details { get; set; }
    }
}
