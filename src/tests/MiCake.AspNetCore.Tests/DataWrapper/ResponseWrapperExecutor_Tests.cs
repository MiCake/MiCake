using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class ResponseWrapperExecutor_Tests
    {
        [Fact]
        public void WrapSuccess_NormalData_ReturnsStandardResponse()
        {
            // Arrange
            var options = new DataWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var originalData = "test data";

            // Act
            var result = executor.WrapSuccess(originalData, httpContext, 200);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("0", response.Code);
            Assert.Equal(originalData, response.Data);
        }

        [Fact]
        public void WrapSuccess_AlreadyWrapped_ReturnsOriginal()
        {
            // Arrange
            var options = new DataWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var wrappedData = new ApiResponse("200", "OK", "data");

            // Act
            var result = executor.WrapSuccess(wrappedData, httpContext, 200);

            // Assert
            Assert.Same(wrappedData, result);
        }

        [Fact]
        public void WrapSuccess_ProblemDetails_WrapDisabled_ReturnsOriginal()
        {
            // Arrange
            var options = new DataWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var problemDetails = new ProblemDetails { Title = "Error" };

            // Act
            var result = executor.WrapSuccess(problemDetails, httpContext, 400);

            // Assert
            Assert.Same(problemDetails, result);
        }

        [Fact]
        public void WrapSuccess_ProblemDetails_WrapEnabled_ReturnsWrapped()
        {
            // Arrange
            var options = new DataWrapperOptions { WrapProblemDetails = true };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var problemDetails = new ProblemDetails { Title = "Error" };

            // Act
            var result = executor.WrapSuccess(problemDetails, httpContext, 400);

            // Assert
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void WrapSuccess_ValidationProblemDetails_WrapDisabled_ReturnsOriginal()
        {
            // Arrange
            var options = new DataWrapperOptions { WrapValidationProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var validationProblem = new ValidationProblemDetails();

            // Act
            var result = executor.WrapSuccess(validationProblem, httpContext, 400);

            // Assert
            Assert.Same(validationProblem, result);
        }

        [Fact]
        public void WrapError_Exception_ReturnsErrorResponse()
        {
            // Arrange
            var options = new DataWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(500);
            var exception = new Exception("Test error");

            // Act
            var result = executor.WrapError(exception, httpContext, 500);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ErrorResponse>(result);
            var errorResponse = result as ErrorResponse;
            Assert.Equal("9999", errorResponse.Code);
            Assert.Equal("Test error", errorResponse.Message);
            Assert.Null(errorResponse.StackTrace);
        }

        [Fact]
        public void WrapError_WithStackTrace_IncludesStackTrace()
        {
            // Arrange
            var options = new DataWrapperOptions { ShowStackTraceWhenError = true };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(500);
            
            // Create an exception with a stack trace by throwing and catching it
            Exception exception = null;
            try
            {
                throw new Exception("Test error");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Act
            var result = executor.WrapError(exception, httpContext, 500);

            // Assert
            var errorResponse = result as ErrorResponse;
            Assert.NotNull(errorResponse);
            Assert.NotNull(errorResponse.StackTrace);
        }

        [Fact]
        public void WrapSuccess_CustomFactory_UsesFactory()
        {
            // Arrange
            var customResponse = new { status = "success", payload = "data" };
            var options = new DataWrapperOptions
            {
                WrapperFactory = new ResponseWrapperFactory
                {
                    SuccessFactory = ctx => customResponse
                }
            };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);

            // Act
            var result = executor.WrapSuccess("original", httpContext, 200);

            // Assert
            Assert.Same(customResponse, result);
        }

        [Fact]
        public void WrapError_CustomFactory_UsesFactory()
        {
            // Arrange
            var customError = new { error = "custom error" };
            var options = new DataWrapperOptions
            {
                WrapperFactory = new ResponseWrapperFactory
                {
                    ErrorFactory = ctx => customError
                }
            };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(500);
            var exception = new Exception("Test");

            // Act
            var result = executor.WrapError(exception, httpContext, 500);

            // Assert
            Assert.Same(customError, result);
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ResponseWrapperExecutor(null));
        }

        private static HttpContext CreateHttpContext(int statusCode)
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = statusCode;
            return context;
        }
    }
}
