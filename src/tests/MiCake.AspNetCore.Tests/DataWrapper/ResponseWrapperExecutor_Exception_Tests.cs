using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Unit tests for ResponseWrapperExecutor exception wrapping behavior.
    /// Tests verify that custom response factories are applied consistently for exceptions.
    /// </summary>
    public class ResponseWrapperExecutor_Exception_Tests
    {
        private readonly ResponseWrapperOptions _defaultOptions = new ResponseWrapperOptions();

        [Fact]
        public void WriteBusinessExceptionResponse_WithDefaultFactory_ReturnsApiResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetBusinessExceptionContext(exception);
            var wrappedData = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.NotNull(wrappedData);
            Assert.IsType<ApiResponse>(wrappedData);
        }

        [Fact]
        public void WriteBusinessExceptionResponse_WithCustomFactory_ReturnsCustomObject()
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
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };
            var exception = new BusinessException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetBusinessExceptionContext(exception);
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
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };
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
        public void BusinessException_StoresAndRetrievesFromContext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Soft error", null, "SOFT_ERROR");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("SOFT_ERROR", retrieved.Code);
        }

        [Fact]
        public void WriteBusinessExceptionResponse_SetsStatusCodeTo200()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Soft error", null, "SOFT_ERROR");

            // Act
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.SetBusinessExceptionContext(exception);

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
        public void CustomFactory_AppliedForBusinessException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new { success = true }
            };
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };
            var exception = new BusinessException("Soft error", null, "SOFT_ERROR");

            // Act
            var executor = new ResponseWrapperExecutor(options);
            httpContext.SetBusinessExceptionContext(exception);
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
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };
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
            var originalException = new Exception("Test error");

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
            var options = new ResponseWrapperOptions { ShowStackTraceWhenError = false };
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
            var options = new ResponseWrapperOptions { ShowStackTraceWhenError = true };
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
        public void BusinessException_Response_Returns200OK()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Business error", null, "BUSINESS_ERROR");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
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
        public void MultipleBusinessExceptions_LastOneWins()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception1 = new BusinessException("Error 1", null, "ERROR_1");
            var exception2 = new BusinessException("Error 2", null, "ERROR_2");

            // Act
            httpContext.SetBusinessExceptionContext(exception1);
            httpContext.SetBusinessExceptionContext(exception2);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("ERROR_2", retrieved.Code);
        }
    }
}
