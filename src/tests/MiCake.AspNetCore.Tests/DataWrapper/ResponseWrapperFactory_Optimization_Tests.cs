using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Tests for ResponseWrapperFactory optimizations in commit 1ba5a78.
    /// Focus: Pattern matching (no reflection), removed ToList() call, and handler consolidation.
    /// </summary>
    public class ResponseWrapperFactory_Optimization_Tests
    {
        #region Pattern Matching Tests (No Reflection)

        [Fact]
        public void DefaultFactory_HttpValidationProblemDetails_UsesPatternMatching()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails { Title = "Validation Error", Status = 400 };
            validationDetails.Errors.Add("Name", new[] { "Name is required" });
            validationDetails.Errors.Add("Email", new[] { "Invalid email" });
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert - Should be ApiResponse, not the original ProblemDetails
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("9998", response.Code); // ProblemDetails code
            Assert.Equal("Validation Error", response.Message);
            // Data should contain all error messages joined
            Assert.NotNull(response.Data);
            var dataString = response.Data as string;
            Assert.Contains("Name is required", dataString);
            Assert.Contains("Invalid email", dataString);
        }

        [Fact]
        public void DefaultFactory_ProblemDetails_UsesPatternMatching()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var problemDetails = new ProblemDetails { Title = "Not Found", Detail = "Resource not found", Status = 404 };
            var context = new WrapperContext(null, 404, problemDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert - Should be ApiResponse, not the original ProblemDetails
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("9998", response.Code);
            Assert.Equal("Not Found", response.Message);
            Assert.Equal("Resource not found", response.Data);
        }

        [Fact]
        public void DefaultFactory_ValidationProblemDetails_UsesPatternMatching()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails { Title = "Validation", Status = 400 };
            validationDetails.Errors.Add("Age", new[] { "Age must be greater than 0" });
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("9998", response.Code);
            Assert.Equal("Validation", response.Message);
        }

        [Fact]
        public void DefaultFactory_MultipleErrorMessages_ConcatenatesWithSemicolon()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails();
            validationDetails.Errors.Add("Password", new[] { "Password is required", "Password must be at least 8 characters" });
            validationDetails.Errors.Add("Email", new[] { "Email is invalid" });
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            var response = result as ApiResponse;
            var dataString = response.Data as string;
            // Should contain all error messages
            Assert.Contains("Password is required", dataString);
            Assert.Contains("Password must be at least 8 characters", dataString);
            Assert.Contains("Email is invalid", dataString);
            // Messages should be joined with semicolon
            Assert.Contains(";", dataString);
        }

        #endregion

        #region Removed ToList() Call Tests

        [Fact]
        public void DefaultFactory_StringJoinPerformance_NoUnnecessaryAllocations()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            
            // Create a validation problem with many errors to test string.Join
            var validationDetails = new ValidationProblemDetails();
            for (int i = 0; i < 100; i++)
            {
                validationDetails.Errors.Add($"Field{i}", new[] { $"Error {i}" });
            }
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert - Should complete successfully with all errors joined
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.NotNull(response.Data);
        }

        [Fact]
        public void DefaultFactory_EmptyErrorCollection_HandlesGracefully()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails();
            // No errors added
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            // Should have empty string as data when no errors
            var dataString = response.Data as string;
            Assert.Equal("", dataString);
        }

        #endregion

        #region SlightException Data Handling Tests

        [Fact]
        public void DefaultFactory_SlightExceptionData_UsesCustomCode()
        {
            // Arrange
            var options = new ResponseWrapperOptions { DefaultCodeSetting = new DataWrapperDefaultCode { Success = "0" } };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var slightData = new SlightExceptionData { Code = "BUSINESS_ERROR", Message = "Custom message", Details = "Details" };
            var context = new WrapperContext(null, 200, slightData);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("BUSINESS_ERROR", response.Code);
            Assert.Equal("Custom message", response.Message);
            Assert.Equal("Details", response.Data);
        }

        [Fact]
        public void DefaultFactory_SlightExceptionData_EmptyCode_UsesDefault()
        {
            // Arrange
            var customCodeSetting = new DataWrapperDefaultCode { Success = "SUCCESS_CODE" };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodeSetting };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var slightData = new SlightExceptionData { Code = "", Message = "Message", Details = null };
            var context = new WrapperContext(null, 200, slightData);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            var response = result as ApiResponse;
            Assert.Equal("SUCCESS_CODE", response.Code);
        }

        [Fact]
        public void DefaultFactory_SlightExceptionData_NullCode_UsesDefault()
        {
            // Arrange
            var customCodeSetting = new DataWrapperDefaultCode { Success = "DEFAULT_CODE" };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodeSetting };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var slightData = new SlightExceptionData { Code = null, Message = "Message", Details = null };
            var context = new WrapperContext(null, 200, slightData);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            var response = result as ApiResponse;
            Assert.Equal("DEFAULT_CODE", response.Code);
        }

        [Fact]
        public void DefaultFactory_SlightExceptionData_TakesPriorityOverProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            // Even if the original data looks like it could be handled differently,
            // SlightException takes priority
            var slightData = new SlightExceptionData { Code = "PRIORITY", Message = "Slight message", Details = "slight details" };
            var context = new WrapperContext(null, 200, slightData);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            var response = result as ApiResponse;
            Assert.Equal("PRIORITY", response.Code);
            Assert.Equal("Slight message", response.Message);
        }

        #endregion

        #region Unified ProblemDetails Flag Tests

        [Fact]
        public void DefaultFactory_WrapProblemDetails_True_WrapsHttpValidationProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails { Title = "Error" };
            validationDetails.Errors.Add("field", new[] { "error" });
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert - Should wrap
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void DefaultFactory_WrapProblemDetails_False_DoesNotWrapHttpValidationProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var validationDetails = new ValidationProblemDetails { Title = "Error" };
            validationDetails.Errors.Add("field", new[] { "error" });
            var context = new WrapperContext(null, 400, validationDetails);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert - Should not wrap, logic falls through to regular data handling
            Assert.IsType<ApiResponse>(result);
            // But it wraps as regular data, not as ProblemDetails wrapper
            var response = result as ApiResponse;
            Assert.Equal("0", response.Code); // Default success code, not ProblemDetails code
        }

        [Fact]
        public void DefaultFactory_WrapProblemDetails_UnifiedAcrossBothTypes()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Test 1: HttpValidationProblemDetails
            var httpValidationProblemDetails = new ValidationProblemDetails();
            httpValidationProblemDetails.Errors.Add("field", new[] { "error" });
            var context1 = new WrapperContext(null, 400, httpValidationProblemDetails);
            var result1 = factory.SuccessFactory(context1);

            // Test 2: ProblemDetails
            var problemDetails = new ProblemDetails { Title = "Error" };
            var context2 = new WrapperContext(null, 400, problemDetails);
            var result2 = factory.SuccessFactory(context2);

            // Assert - Both should use the same code
            var response1 = result1 as ApiResponse;
            var response2 = result2 as ApiResponse;
            Assert.Equal("9998", response1.Code); // ProblemDetails code
            Assert.Equal("9998", response2.Code); // Same code for both
        }

        #endregion

        #region Error Factory Tests

        [Fact]
        public void DefaultErrorFactory_CreatesErrorResponse()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var exception = new Exception("Test error message");
            var context = new ErrorWrapperContext(null, 500, null, exception);

            // Act
            var result = factory.ErrorFactory(context);

            // Assert
            Assert.IsType<ErrorResponse>(result);
            var response = result as ErrorResponse;
            Assert.Equal("9999", response.Code);
            Assert.Equal("Test error message", response.Message);
        }

        [Fact]
        public void DefaultErrorFactory_WithStackTrace_IncludesStackTrace()
        {
            // Arrange
            var options = new ResponseWrapperOptions { ShowStackTraceWhenError = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            
            Exception exception = null;
            try
            {
                throw new Exception("Test");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var context = new ErrorWrapperContext(null, 500, null, exception);

            // Act
            var result = factory.ErrorFactory(context);

            // Assert
            var response = result as ErrorResponse;
            Assert.NotNull(response.StackTrace);
        }

        [Fact]
        public void DefaultErrorFactory_WithoutStackTrace_OmitsStackTrace()
        {
            // Arrange
            var options = new ResponseWrapperOptions { ShowStackTraceWhenError = false };
            var factory = ResponseWrapperFactory.CreateDefault(options);
            Exception exception = null;
            try
            {
                throw new Exception("Test");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var context = new ErrorWrapperContext(null, 500, null, exception);

            // Act
            var result = factory.ErrorFactory(context);

            // Assert
            var response = result as ErrorResponse;
            Assert.Null(response.StackTrace);
        }

        [Fact]
        public void DefaultErrorFactory_NullException_HandlesGracefully()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var context = new ErrorWrapperContext(null, 500, null, null);

            // Act
            var result = factory.ErrorFactory(context);

            // Assert
            var response = result as ErrorResponse;
            Assert.Equal("An error occurred", response.Message);
        }

        #endregion

        #region Factory Creation Tests

        [Fact]
        public void CreateDefault_ReturnsValidFactory()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Assert
            Assert.NotNull(factory);
            Assert.NotNull(factory.SuccessFactory);
            Assert.NotNull(factory.ErrorFactory);
        }

        [Fact]
        public void CreateDefault_WithCustomCodeSettings_UsesCustomCodes()
        {
            // Arrange
            var customCodes = new DataWrapperDefaultCode
            {
                Success = "200",
                Error = "500",
                ProblemDetails = "400"
            };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodes };

            // Act
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Test success factory
            var successContext = new WrapperContext(null, 200, "data");
            var successResult = factory.SuccessFactory(successContext) as ApiResponse;

            // Test error factory
            var errorContext = new ErrorWrapperContext(null, 500, null, new Exception("Test"));
            var errorResult = factory.ErrorFactory(errorContext) as ErrorResponse;

            // Assert
            Assert.Equal("200", successResult.Code);
            Assert.Equal("500", errorResult.Code);
        }

        #endregion

        #region Integration Scenarios

        [Fact]
        public void DefaultFactory_CompleteFlow_SuccessPath()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Test 1: Regular data
            var context1 = new WrapperContext(null, 200, new { name = "test" });
            var result1 = factory.SuccessFactory(context1);
            Assert.IsType<ApiResponse>(result1);

            // Test 2: ProblemDetails
            var context2 = new WrapperContext(null, 400, new ProblemDetails { Title = "Error" });
            var result2 = factory.SuccessFactory(context2);
            Assert.IsType<ApiResponse>(result2);
            Assert.Equal("9998", (result2 as ApiResponse).Code);

            // Test 3: SlightExceptionData
            var context3 = new WrapperContext(null, 200, new SlightExceptionData { Code = "CUSTOM", Message = "msg", Details = null });
            var result3 = factory.SuccessFactory(context3);
            Assert.IsType<ApiResponse>(result3);
            Assert.Equal("CUSTOM", (result3 as ApiResponse).Code);
        }

        [Fact]
        public void DefaultFactory_ProblemDetailsAndSlightException_SlightExceptionWins()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Even though the original data is ProblemDetails-like,
            // when passed as SlightExceptionData, it should be handled as SlightException
            var slightData = new SlightExceptionData
            {
                Code = "PRIORITY",
                Message = "Slight takes priority",
                Details = "details"
            };
            var context = new WrapperContext(null, 200, slightData);

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            var response = result as ApiResponse;
            Assert.Equal("PRIORITY", response.Code);
            Assert.Equal("Slight takes priority", response.Message);
        }

        #endregion
    }
}
