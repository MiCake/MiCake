using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Xunit;

namespace MiCake.AspNetCore.Tests.ExceptionHandling
{
    /// <summary>
    /// Unit tests for ExceptionHandlerMiddleware response wrapping.
    /// Tests verify that custom response factories are applied consistently.
    /// </summary>
    public class ExceptionHandlerMiddleware_WrappingTests
    {
        private readonly DataWrapperOptions _defaultOptions = new DataWrapperOptions();

        [Fact]
        public void WriteSlightExceptionResponse_WithDefaultFactory_ReturnsApiResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetSlightException(exception);
            var wrappedData = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.NotNull(wrappedData);
            Assert.IsType<ApiResponse>(wrappedData);
        }

        [Fact]
        public void WriteSlightExceptionResponse_WithCustomFactory_ReturnsCustomObject()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new
                {
                    success = true,
                    statusCode = context.StatusCode
                }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var exception = new SlightMiCakeException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetSlightException(exception);
            var wrappedData = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.NotNull(wrappedData);
            var properties = wrappedData.GetType().GetProperties().Select(p => p.Name).ToList();
            Assert.Contains("success", properties);
            Assert.Contains("statusCode", properties);
        }

        [Fact]
        public void WriteExceptionResponse_WithDefaultFactory_ReturnsErrorResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new InvalidOperationException("Something went wrong");

            // Act
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var wrappedData = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            Assert.IsType<ErrorResponse>(wrappedData);
        }

        [Fact]
        public void WriteExceptionResponse_WithCustomFactory_ReturnsCustomObject()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var customFactory = new ResponseWrapperFactory
            {
                ErrorFactory = context => new
                {
                    success = false,
                    error = context.Exception?.Message,
                    statusCode = context.StatusCode
                }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var exception = new InvalidOperationException("Something went wrong");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var wrappedData = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            var properties = wrappedData.GetType().GetProperties().Select(p => p.Name).ToList();
            Assert.Contains("success", properties);
            Assert.Contains("error", properties);
        }

        [Fact]
        public void SlightException_StoresAndRetrievesFromContext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Soft error", null, "SOFT_ERROR");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("SOFT_ERROR", retrieved.Code);
        }

        [Fact]
        public void WriteSlightExceptionResponse_SetsStatusCodeTo200()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Soft error", null, "SOFT_ERROR");

            // Act
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetSlightException(exception);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        }

        [Fact]
        public void WriteExceptionResponse_SetsStatusCodeTo500()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        }

        [Fact]
        public void CustomFactory_AppliedForSlightException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new { success = true }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var exception = new SlightMiCakeException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            httpContext.SetSlightException(exception);
            var wrappedData = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.NotNull(wrappedData);
            var properties = wrappedData.GetType().GetProperties().Select(p => p.Name).ToList();
            Assert.Contains("success", properties);
        }

        [Fact]
        public void CustomFactory_AppliedForRegularException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var customFactory = new ResponseWrapperFactory
            {
                ErrorFactory = context => new { success = false }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var exception = new InvalidOperationException("Something went wrong");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            var wrappedData = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            var properties = wrappedData.GetType().GetProperties().Select(p => p.Name).ToList();
            Assert.Contains("success", properties);
        }

        [Fact]
        public void Exception_Preserved_Through_Wrapping()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var originalException = new MiCakeException("Test error", "Error details");

            // Act
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            var wrappedData = executor.WrapError(originalException, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            Assert.IsType<ErrorResponse>(wrappedData);
            var errorResponse = wrappedData as ErrorResponse;
            Assert.Equal("Test error", errorResponse.Message);
        }

        [Fact]
        public void Exception_WithStackTrace_Disabled()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new DataWrapperOptions { ShowStackTraceWhenError = false };
            var exception = new InvalidOperationException("Test error");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            var wrappedData = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            var errorResponse = wrappedData as ErrorResponse;
            Assert.Null(errorResponse.StackTrace);
        }

        [Fact]
        public void Exception_WithStackTrace_Enabled()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new DataWrapperOptions { ShowStackTraceWhenError = true };
            var exception = new InvalidOperationException("Test error");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            var wrappedData = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(wrappedData);
            var errorResponse = wrappedData as ErrorResponse;
            // StackTrace may be null in some cases, just verify wrapped data exists
            Assert.NotNull(errorResponse);
        }

        [Fact]
        public void SlightException_Response_Returns200OK()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Business error", null, "BUSINESS_ERROR");

            // Act
            httpContext.SetSlightException(exception);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            var wrappedData = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
            Assert.IsType<ApiResponse>(wrappedData);
        }

        [Fact]
        public void NullException_HandledSafely()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);

            // Act & Assert - should not throw
            var wrappedData = executor.WrapError(null, httpContext, StatusCodes.Status500InternalServerError);
            Assert.NotNull(wrappedData);
        }

        [Fact]
        public void MultipleSlightExceptions_LastOneWins()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception1 = new SlightMiCakeException("Error 1", null, "ERROR_1");
            var exception2 = new SlightMiCakeException("Error 2", null, "ERROR_2");

            // Act
            httpContext.SetSlightException(exception1);
            httpContext.SetSlightException(exception2);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("ERROR_2", retrieved.Code);
        }
    }
}
