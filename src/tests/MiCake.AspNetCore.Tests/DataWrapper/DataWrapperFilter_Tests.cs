using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
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
        public DataWrapperFilter_Tests()
        {
        }

        [Fact]
        public async Task DataWrapperFilter_ObjectResult_UseWrapper()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ObjectResult objectResult = new(1234);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
            });
            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult);
            var wrapperFilter = new DataWrapperFilter(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            //assert
            Assert.NotNull(resultExecutingContext.Result);
            Assert.IsAssignableFrom<ObjectResult>(resultExecutingContext.Result);
            Assert.IsAssignableFrom<IResultDataWrapper>(((ObjectResult)resultExecutingContext.Result).Value);
        }

        [Fact]
        public async Task DataWrapperFilter_NotObjectResult()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var noFoundResult = new NotFoundResult();
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
            });
            var resultExecutingContext = GetResourceExecutingContext(httpContext, noFoundResult);
            var wrapperFilter = new DataWrapperFilter(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            //assert
            var resultInfo = resultExecutingContext.Result as NotFoundResult;
            Assert.NotNull(resultInfo);
        }

        [Fact]
        public async Task DataWrapperFilter_NotWrapStatueCodeOption()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var objResult = new ObjectResult("1234");
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
                {
                    NoWrapStatusCode = new List<int>() { 200 }
                }
            });
            var resultExecutingContext = GetResourceExecutingContext(httpContext, objResult);
            var wrapperFilter = new DataWrapperFilter(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            //assert
            var resultInfo = resultExecutingContext.Result as ObjectResult;
            Assert.NotNull(resultInfo);
            Assert.False(resultInfo.Value is IResultDataWrapper);
        }

        [Fact]
        public async Task DataWrapperFilter_ObjectResultStatusCodeIsNull()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var objResult = new ObjectResult("1234") { StatusCode = null };
            var options = Options.Create(new MiCakeAspNetOptions());
            var resultExecutingContext = GetResourceExecutingContext(httpContext, objResult);
            var wrapperFilter = new DataWrapperFilter(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            //assert
            var resultInfo = resultExecutingContext.Result as ObjectResult;
            Assert.NotNull(resultInfo);
            Assert.Equal(200, (resultInfo.Value as ApiResponse).StatusCode);
        }

        [Fact]
        public async Task DataWrapperFilter_ObjectResultStatusCodeNotNull()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var objResult = new OkObjectResult("1234") { StatusCode = 203 };
            var options = Options.Create(new MiCakeAspNetOptions());
            var resultExecutingContext = GetResourceExecutingContext(httpContext, objResult);
            var wrapperFilter = new DataWrapperFilter(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            //assert
            var resultInfo = resultExecutingContext.Result as ObjectResult;
            Assert.NotNull(resultInfo);
            Assert.Equal(203, (resultInfo.Value as ApiResponse).StatusCode);
        }

        private ResultExecutingContext GetResourceExecutingContext(HttpContext httpContext, IActionResult result)
        {
            return new ResultExecutingContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                result,
                controller: new object());
        }

        private ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
        {
            return new ResultExecutedContext(context, context.Filters, context.Result, context.Controller);
        }

        private HttpContext CreateFakeHttpContext(string method, int statusCode)
        {
            var fakeHttpContext = new DefaultHttpContext();
            fakeHttpContext.Request.Method = method;
            fakeHttpContext.Response.StatusCode = statusCode;

            return fakeHttpContext;
        }
    }
}
