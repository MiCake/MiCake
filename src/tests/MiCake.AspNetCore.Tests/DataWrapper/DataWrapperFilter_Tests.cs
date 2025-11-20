using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class DataWrapperFilter_Tests
    {
        [Fact]
        public async Task DataWrapperFilter_ObjectResult_UseWrapper()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ObjectResult objectResult = new(1234);

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult);
            var wrapperFilter = new DataWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert
            Assert.NotNull(resultExecutingContext.Result);
            Assert.IsAssignableFrom<ObjectResult>(resultExecutingContext.Result);
            Assert.IsAssignableFrom<IResponseWrapper>(((ObjectResult)resultExecutingContext.Result).Value);
            
            // The non-generic StandardResponse is used for primitives
            var standardResponse = ((ObjectResult)resultExecutingContext.Result).Value as ApiResponse;
            Assert.NotNull(standardResponse);
            Assert.Equal(1234, standardResponse.Data);
        }

        [Fact]
        public async Task DataWrapperFilter_NotObjectResult()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ContentResult contentResult = new() { Content = "test" };

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var resultExecutingContext = GetResourceExecutingContext(httpContext, contentResult);
            var wrapperFilter = new DataWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should not wrap non-ObjectResult
            Assert.NotNull(resultExecutingContext.Result);
            Assert.IsType<ContentResult>(resultExecutingContext.Result);
        }

        [Fact]
        public async Task DataWrapperFilter_AlreadyWrapped_DoesNotDoubleWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var alreadyWrappedData = new ApiResponse<string>
            {
                Code = "0",
                Message = "Success",
                Data = "test"
            };
            ObjectResult objectResult = new(alreadyWrappedData);

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult);
            var wrapperFilter = new DataWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should not double-wrap
            Assert.NotNull(resultExecutingContext.Result);
            var resultValue = ((ObjectResult)resultExecutingContext.Result).Value;
            Assert.IsType<ApiResponse<string>>(resultValue);
            Assert.Equal("test", ((ApiResponse<string>)resultValue).Data);
        }

        [Fact]
        public async Task DataWrapperFilter_IgnoreStatusCode_DoesNotWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 404);
            ObjectResult objectResult = new(new { Error = "NotFound" }) { StatusCode = 404 };

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
                {
                    IgnoreStatusCodes = new List<int> { 404 }
                }
            });

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult);
            var wrapperFilter = new DataWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should not wrap ignored status codes
            Assert.NotNull(resultExecutingContext.Result);
            var resultValue = ((ObjectResult)resultExecutingContext.Result).Value;
            Assert.False(resultValue is IResponseWrapper);
        }

        private static ResultExecutingContext GetResourceExecutingContext(HttpContext httpContext, IActionResult actionResult)
        {
            return new ResultExecutingContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                actionResult,
                new object());
        }

        private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext resultExecutingContext)
        {
            return new ResultExecutedContext(
                resultExecutingContext,
                resultExecutingContext.Filters,
                resultExecutingContext.Result,
                resultExecutingContext.Controller);
        }

        private static HttpContext CreateFakeHttpContext(string method, int statusCode)
        {
            var fakeHttpContext = new DefaultHttpContext();
            fakeHttpContext.Request.Method = method;
            fakeHttpContext.Response.StatusCode = statusCode;

            return fakeHttpContext;
        }
    }
}
