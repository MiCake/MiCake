using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Tests for ResponseWrapperExecutor optimizations in commit 1ba5a78.
    /// Focus: C# 12 primary constructor, IsProblemDetails() helper method, and BusinessException flow improvements.
    /// </summary>
    public class ResponseWrapperExecutor_Optimization_Tests
    {
        #region IsProblemDetails Helper Tests

        [Fact]
        public void IsProblemDetails_WithProblemDetails_ReturnsTrue()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var problemDetails = new ProblemDetails { Title = "Validation Error" };

            // Act - Wrap success which triggers IsProblemDetails internally
            var result = executor.WrapSuccess(problemDetails, httpContext, 400);

            // Assert - Should not wrap because WrapProblemDetails is now default true, so it should wrap
            Assert.NotNull(result);
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void IsProblemDetails_WithHttpValidationProblemDetails_ReturnsTrue()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var validationProblemDetails = new HttpValidationProblemDetails
            {
                Title = "Validation Error",
                Status = 400
            };
            validationProblemDetails.Errors.Add("field", new[] { "error message" });

            // Act - Should return original because WrapProblemDetails is false
            var result = executor.WrapSuccess(validationProblemDetails, httpContext, 400);

            // Assert
            Assert.Same(validationProblemDetails, result);
        }

        [Fact]
        public void IsProblemDetails_WithValidationProblemDetails_ReturnsTrue()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var validationProblemDetails = new ValidationProblemDetails
            {
                Title = "Validation Error",
                Status = 400
            };
            validationProblemDetails.Errors.Add("field", new[] { "error message" });

            // Act - Should return original because WrapProblemDetails is false
            var result = executor.WrapSuccess(validationProblemDetails, httpContext, 400);

            // Assert
            Assert.Same(validationProblemDetails, result);
        }

        [Fact]
        public void IsProblemDetails_WithRegularObject_ReturnsFalse()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var regularData = new { name = "test" };

            // Act
            var result = executor.WrapSuccess(regularData, httpContext, 200);

            // Assert - Should wrap because it's not ProblemDetails
            Assert.IsType<ApiResponse>(result);
        }

        #endregion

        #region BusinessException Flow Tests

        [Fact]
        public void WrapSuccess_WithBusinessException_UsesBusinessExceptionDataFactory()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var businessException = new BusinessException("Custom message", details: "{\"detail\": \"info\"}", code: "CUSTOM_CODE");
            httpContext.SetBusinessExceptionContext(businessException);

            // Act
            var result = executor.WrapSuccess("original data", httpContext, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("CUSTOM_CODE", response.Code);
            Assert.Equal("Custom message", response.Message);
            Assert.NotNull(response.Data);
        }

        [Fact]
        public void WrapSuccess_WithBusinessExceptionNullDetails_HandlesGracefully()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var businessException = new BusinessException("Custom message", details: null, code: "CUSTOM_CODE");
            httpContext.SetBusinessExceptionContext(businessException);

            // Act
            var result = executor.WrapSuccess("original data", httpContext, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("CUSTOM_CODE", response.Code);
            Assert.Equal("Custom message", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public void WrapSuccess_WithBusinessExceptionEmptyCode_UsesDefaultCode()
        {
            // Arrange
            var customCodeSetting = new DataWrapperDefaultCode { Success = "200" };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodeSetting };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var businessException = new BusinessException("Custom message", details: null, code: "");
            httpContext.SetBusinessExceptionContext(businessException);

            // Act
            var result = executor.WrapSuccess("original data", httpContext, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("200", response.Code); // Uses default code
        }

        [Fact]
        public void WrapSuccess_WithBusinessExceptionComplexData_IncludesAllData()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var businessException = new BusinessException("User already exists", details: "{\"userId\": 123, \"userName\": \"John\", \"status\": \"active\"}", code: "USER_EXISTS");
            httpContext.SetBusinessExceptionContext(businessException);

            // Act
            var result = executor.WrapSuccess("original data", httpContext, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("USER_EXISTS", response.Code);
            Assert.Equal("User already exists", response.Message);
            // Data should be the details object
            var dataObj = response.Data;
            Assert.NotNull(dataObj);
        }

        #endregion

        #region WrapProblemDetails Unified Flag Tests

        [Fact]
        public void WrapProblemDetails_DefaultTrue_WrapsHttpValidationProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions(); // Default: WrapProblemDetails = true
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var validationProblemDetails = new HttpValidationProblemDetails();
            validationProblemDetails.Errors.Add("field", new[] { "error" });

            // Act
            var result = executor.WrapSuccess(validationProblemDetails, httpContext, 400);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("9998", response.Code); // ProblemDetails code
        }

        [Fact]
        public void WrapProblemDetails_DefaultTrue_WrapsProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions(); // Default: WrapProblemDetails = true
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var problemDetails = new ProblemDetails { Title = "Error", Detail = "Something went wrong" };

            // Act
            var result = executor.WrapSuccess(problemDetails, httpContext, 400);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("9998", response.Code);
        }

        [Fact]
        public void WrapProblemDetails_False_DoesNotWrapHttpValidationProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var validationProblemDetails = new HttpValidationProblemDetails();
            validationProblemDetails.Errors.Add("field", new[] { "error" });

            // Act
            var result = executor.WrapSuccess(validationProblemDetails, httpContext, 400);

            // Assert
            Assert.Same(validationProblemDetails, result);
        }

        [Fact]
        public void WrapProblemDetails_False_DoesNotWrapProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(400);
            var problemDetails = new ProblemDetails { Title = "Error", Detail = "Something went wrong" };

            // Act
            var result = executor.WrapSuccess(problemDetails, httpContext, 400);

            // Assert
            Assert.Same(problemDetails, result);
        }

        [Fact]
        public void WrapProblemDetails_UnifiedFlagAppliesToBothTypes()
        {
            // Arrange
            var optionsWrap = new ResponseWrapperOptions { WrapProblemDetails = true };
            var optionsDontWrap = new ResponseWrapperOptions { WrapProblemDetails = false };
            var executorWrap = new ResponseWrapperExecutor(optionsWrap);
            var executorDontWrap = new ResponseWrapperExecutor(optionsDontWrap);
            var httpContext1 = CreateHttpContext(400);
            var httpContext2 = CreateHttpContext(400);

            // Act & Assert - HttpValidationProblemDetails
            var httpValidationProblemDetails = new HttpValidationProblemDetails();
            httpValidationProblemDetails.Errors.Add("field", new[] { "error" });

            var resultWrap = executorWrap.WrapSuccess(httpValidationProblemDetails, httpContext1, 400);
            Assert.IsType<ApiResponse>(resultWrap);

            var resultDontWrap = executorDontWrap.WrapSuccess(httpValidationProblemDetails, httpContext2, 400);
            Assert.Same(httpValidationProblemDetails, resultDontWrap);

            // Act & Assert - ProblemDetails
            var problemDetails = new ProblemDetails { Title = "Error" };

            var resultWrap2 = executorWrap.WrapSuccess(problemDetails, CreateHttpContext(400), 400);
            Assert.IsType<ApiResponse>(resultWrap2);

            var resultDontWrap2 = executorDontWrap.WrapSuccess(problemDetails, CreateHttpContext(400), 400);
            Assert.Same(problemDetails, resultDontWrap2);
        }

        #endregion

        #region Primary Constructor Tests

        [Fact]
        public void Constructor_WithValidOptions_Succeeds()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            var executor = new ResponseWrapperExecutor(options);

            // Assert
            Assert.NotNull(executor);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ResponseWrapperExecutor(null));
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithCustomFactory_UsesCustomFactory()
        {
            // Arrange
            var customResponse = new { custom = "response" };
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = ctx => customResponse
            };
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };

            // Act
            var executor = new ResponseWrapperExecutor(options);
            var result = executor.WrapSuccess("data", CreateHttpContext(200), 200);

            // Assert
            Assert.Same(customResponse, result);
        }

        #endregion

        #region Response Wrapper Interface Tests

        [Fact]
        public void WrapSuccess_WithIResponseWrapper_ReturnsOriginal()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var alreadyWrapped = new ApiResponse("0", null, "data");

            // Act
            var result = executor.WrapSuccess(alreadyWrapped, httpContext, 200);

            // Assert
            Assert.Same(alreadyWrapped, result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void WrapSuccess_WithNullHttpContext_HandlesGracefully()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);

            // Act
            var result = executor.WrapSuccess("data", null, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void WrapSuccess_WithNullData_WrapsAsNull()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);

            // Act
            var result = executor.WrapSuccess(null, httpContext, 200);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Null(response.Data);
        }

        [Fact]
        public void WrapError_WithNullException_HandlesGracefully()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(500);

            // Act
            var result = executor.WrapError(null, httpContext, 500);

            // Assert
            Assert.IsType<ErrorResponse>(result);
            var errorResponse = result as ErrorResponse;
            Assert.Equal("An error occurred", errorResponse.Message);
        }

        [Fact]
        public void WrapError_WithCustomCodeSetting_UsesCustomCode()
        {
            // Arrange
            var customCodeSetting = new DataWrapperDefaultCode { Error = "5000" };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodeSetting };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(500);
            var exception = new Exception("Test error");

            // Act
            var result = executor.WrapError(exception, httpContext, 500);

            // Assert
            Assert.IsType<ErrorResponse>(result);
            var errorResponse = result as ErrorResponse;
            Assert.Equal("5000", errorResponse.Code);
        }

        #endregion

        #region Integration Scenarios

        [Fact]
        public void MultipleWrappingScenarios_ConsistencyAcrossTypes()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var executor = new ResponseWrapperExecutor(options);

            // Test 1: Regular data
            var result1 = executor.WrapSuccess("data", CreateHttpContext(200), 200);
            Assert.IsType<ApiResponse>(result1);

            // Test 2: Problem Details
            var problemDetails = new ProblemDetails { Title = "Error" };
            var result2 = executor.WrapSuccess(problemDetails, CreateHttpContext(400), 400);
            Assert.IsType<ApiResponse>(result2);

            // Test 3: HttpValidationProblemDetails
            var validationDetails = new HttpValidationProblemDetails();
            validationDetails.Errors.Add("field", new[] { "error" });
            var result3 = executor.WrapSuccess(validationDetails, CreateHttpContext(400), 400);
            Assert.IsType<ApiResponse>(result3);

            // Test 4: Already wrapped
            var alreadyWrapped = new ApiResponse("0", null, "data");
            var result4 = executor.WrapSuccess(alreadyWrapped, CreateHttpContext(200), 200);
            Assert.Same(alreadyWrapped, result4);

            // Test 5: Error
            var result5 = executor.WrapError(new Exception("Error"), CreateHttpContext(500), 500);
            Assert.IsType<ErrorResponse>(result5);
        }

        [Fact]
        public void WrapSuccess_ProblemDetailsWithBusinessException_BusinessExceptionTakesPriority()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var executor = new ResponseWrapperExecutor(options);
            var httpContext = CreateHttpContext(200);
            var businessException = new BusinessException("Slight error", details: null, code: "SLIGHT");
            httpContext.SetBusinessExceptionContext(businessException);
            var problemDetails = new ProblemDetails { Title = "Problem" }; // This should be ignored

            // Act
            var result = executor.WrapSuccess(problemDetails, httpContext, 200);

            // Assert - Should use BusinessException data, not ProblemDetails
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("SLIGHT", response.Code);
            Assert.Equal("Slight error", response.Message);
        }

        #endregion

        private static HttpContext CreateHttpContext(int statusCode)
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = statusCode;
            return context;
        }
    }
}
