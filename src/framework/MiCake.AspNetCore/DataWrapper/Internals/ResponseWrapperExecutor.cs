using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MiCake.AspNetCore.DataWrapper.Internals
{
    /// <summary>
    /// Simplified executor for wrapping responses.
    /// Focuses solely on wrapping logic using factory pattern.
    /// </summary>
    internal class ResponseWrapperExecutor
    {
        private readonly DataWrapperOptions _options;

        public ResponseWrapperExecutor(DataWrapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Wraps a successful response using the configured factory.
        /// </summary>
        public object WrapSuccess(object originalData, HttpContext httpContext, int statusCode)
        {
            if (originalData is IResponseWrapper)
                return originalData;

            // Handle ProblemDetails
            if (originalData is ProblemDetails && !_options.WrapProblemDetails)
                return originalData;

            // Handle ValidationProblemDetails 
            if (originalData is ValidationProblemDetails && !_options.WrapValidationProblemDetails)
                return originalData;

            // Handle SlightException - treated as successful response with custom code
            if (TryGetSlightException(httpContext, out var slightException))
            {
                // Create a special object that contains exception details for the factory
                var slightExceptionData = new SlightExceptionData
                {
                    Code = slightException.Code,
                    Message = slightException.Message,
                    Details = slightException.Details
                };

                var slightExceptionContext = new WrapperContext(httpContext, statusCode, slightExceptionData);
                return _options.GetOrCreateFactory().SuccessFactory(slightExceptionContext);
            }

            var context = new WrapperContext(httpContext, statusCode, originalData);
            return _options.GetOrCreateFactory().SuccessFactory(context);
        }

        /// <summary>
        /// Wraps an error response using the configured factory.
        /// </summary>
        public object WrapError(Exception exception, HttpContext httpContext, int statusCode, object originalData = null)
        {
            var context = new ErrorWrapperContext(httpContext, statusCode, originalData, exception);
            return _options.GetOrCreateFactory().ErrorFactory(context);
        }

        /// <summary>
        /// Checks if the current exception is a SlightException stored in HttpContext.
        /// </summary>
        private static bool TryGetSlightException(HttpContext httpContext, out SlightMiCakeException exception)
        {
            exception = null;
            if (httpContext?.Items != null &&
                httpContext.Items.TryGetValue("MiCake.SlightException", out var item) &&
                item is SlightMiCakeException slightEx)
            {
                exception = slightEx;
                return true;
            }
            return false;
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
        public string Code { get; set; }

        /// <summary>
        /// Exception message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Additional details from the exception.
        /// </summary>
        public object Details { get; set; }
    }
}
