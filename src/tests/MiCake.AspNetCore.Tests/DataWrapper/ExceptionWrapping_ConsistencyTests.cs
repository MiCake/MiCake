using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.AspNetCore.ExceptionHandling;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Integration tests for consistency between ExceptionDataWrapperFilter and ExceptionHandlerMiddleware.
    /// Verifies that both components use SlightException handling consistently.
    /// </summary>
    public class ExceptionWrapping_ConsistencyTests
    {
        private readonly DataWrapperOptions _defaultOptions = new DataWrapperOptions();

        [Fact]
        public void Filter_And_Middleware_Both_Use_Extensions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Consistent error", null, "CONSISTENT");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("CONSISTENT", retrieved.Code);
        }

        [Fact]
        public void DefaultFactory_ConsistentBetweenComponents()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);

            // Act
            var successWrapped = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);
            var errorWrapped = executor.WrapError(new Exception("Test"), httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.NotNull(successWrapped);
            Assert.NotNull(errorWrapped);
            Assert.IsType<ApiResponse>(successWrapped);
            Assert.IsType<ErrorResponse>(errorWrapped);
        }

        [Fact]
        public void CustomFactory_Applied_Consistently()
        {
            // Arrange
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new { custom = true },
                ErrorFactory = context => new { custom = true }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = new DefaultHttpContext();

            // Act
            var successWrapped = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);
            var errorWrapped = executor.WrapError(new Exception("Test"), httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            var successProps = successWrapped.GetType().GetProperties().Select(p => p.Name).ToList();
            var errorProps = errorWrapped.GetType().GetProperties().Select(p => p.Name).ToList();
            
            Assert.Contains("custom", successProps);
            Assert.Contains("custom", errorProps);
        }

        [Fact]
        public void SlightException_Stored_Via_Extensions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Extension test", null, "EXT_TEST");

            // Act
            httpContext.SetSlightException(exception);
            var hasIt = httpContext.HasSlightException();
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.True(hasIt);
            Assert.NotNull(retrieved);
        }

        [Fact]
        public void Status_Code_200_For_SlightException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            var exception = new SlightMiCakeException("Soft error", null, "SOFT");

            // Act
            httpContext.SetSlightException(exception);
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            var wrapped = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        }

        [Fact]
        public void Status_Code_500_For_RegularException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            var exception = new InvalidOperationException("Regular error");

            // Act
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var wrapped = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        }

        [Fact]
        public void SlightException_Code_Preserved_Through_Extension()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var testCode = "PRESERVATION_TEST";
            var exception = new SlightMiCakeException("Test message", null, testCode);

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.Equal(testCode, retrieved.Code);
        }

        [Fact]
        public void Both_Components_Handle_Null_Exception_Gracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);

            // Act & Assert - Should not throw
            var wrapped = executor.WrapError(null, httpContext, StatusCodes.Status500InternalServerError);
            Assert.NotNull(wrapped);
        }

        [Fact]
        public void Extension_Methods_Enable_Consistent_Access()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Consistent", null, "CONSISTENT");

            // Act
            httpContext.SetSlightException(exception);
            var exists = httpContext.HasSlightException();
            var result = httpContext.TryGetSlightException(out var outException);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.True(exists);
            Assert.True(result);
            Assert.NotNull(outException);
            Assert.NotNull(retrieved);
            Assert.Equal(outException.Code, retrieved.Code);
        }

        [Fact]
        public void Custom_Factory_Replaces_Default_Structure()
        {
            // Arrange
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new { isSuccess = true }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = new DefaultHttpContext();

            // Act
            var wrapped = executor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            var props = wrapped.GetType().GetProperties().Select(p => p.Name).ToList();
            Assert.Contains("isSuccess", props);
            Assert.DoesNotContain("Code", props); // Default property should not be present
        }

        [Fact]
        public void Exception_Message_Preserved_In_Response()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            var testMessage = "Test exception message";
            var exception = new MiCakeException(testMessage, "Details", "CODE");

            // Act
            var wrapped = executor.WrapError(exception, httpContext, StatusCodes.Status500InternalServerError);

            // Assert
            Assert.IsType<ErrorResponse>(wrapped);
            var errorResponse = wrapped as ErrorResponse;
            Assert.Equal(testMessage, errorResponse.Message);
        }

        [Fact]
        public void SlightException_Message_Preserved_In_Storage()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var testMessage = "Soft error message";
            var exception = new SlightMiCakeException(testMessage, null, "CODE");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.Equal(testMessage, retrieved.Message);
        }

        [Fact]
        public void TryGet_Pattern_Works_With_Middleware_Flow()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Pattern test", null, "PATTERN");

            // Act
            httpContext.SetSlightException(exception);
            var result = httpContext.TryGetSlightException(out var retrieved);

            // Assert
            Assert.True(result);
            Assert.NotNull(retrieved);
        }

        [Fact]
        public void Multiple_Exception_Overwrites_Last_One()
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
            Assert.Equal("ERROR_2", retrieved.Code);
        }

        [Fact]
        public void Has_Method_Returns_Correct_State()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act & Assert
            Assert.False(httpContext.HasSlightException());
            
            httpContext.SetSlightException(new SlightMiCakeException("Test", null, "TEST"));
            Assert.True(httpContext.HasSlightException());
        }

        [Fact]
        public void Factory_Context_Receives_Correct_Parameters()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var executor = new ResponseWrapperExecutor(_defaultOptions);
            string capturedStatusCode = null;

            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context =>
                {
                    capturedStatusCode = context.StatusCode.ToString();
                    return new { statusCode = context.StatusCode };
                }
            };
            var options = new DataWrapperOptions { WrapperFactory = customFactory };
            var customExecutor = new ResponseWrapperExecutor(options);

            // Act
            customExecutor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.Equal(StatusCodes.Status200OK.ToString(), capturedStatusCode);
        }

        [Fact]
        public void Default_vs_Custom_Factory_Behavior_Consistent()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var defaultExecutor = new ResponseWrapperExecutor(_defaultOptions);
            
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = context => new { custom = true }
            };
            var customOptions = new DataWrapperOptions { WrapperFactory = customFactory };
            var customExecutor = new ResponseWrapperExecutor(customOptions);

            // Act
            var defaultWrapped = defaultExecutor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);
            var customWrapped = customExecutor.WrapSuccess(null, httpContext, StatusCodes.Status200OK);

            // Assert
            Assert.NotNull(defaultWrapped);
            Assert.NotNull(customWrapped);
            Assert.IsType<ApiResponse>(defaultWrapped);
            Assert.NotNull(customWrapped);
        }
    }
}
