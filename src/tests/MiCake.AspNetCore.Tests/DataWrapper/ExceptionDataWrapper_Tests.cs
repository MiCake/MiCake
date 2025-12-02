using Castle.Core.Logging;
using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class ExceptionDataWrapper_Tests
    {
        public ExceptionDataWrapper_Tests()
        {
        }

        [Fact]
        public async Task ExceptionDataWrapper_SoftException()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new ResponseWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new BusinessException("Test exception"));
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLoggerFactory.Instance.CreateLogger<ExceptionResponseWrapperFilter>());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.NotNull(result);

            var standardResponse = result.Value as ApiResponse;
            Assert.NotNull(standardResponse);
            Assert.Equal("Test exception", standardResponse.Message);
        }

        [Fact]
        public async Task ExceptionDataWrapper_NormalException()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new ResponseWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new Exception("Test exception"));
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLoggerFactory.Instance.CreateLogger<ExceptionResponseWrapperFilter>());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert - NEW BEHAVIOR: regular exceptions ARE now wrapped and handled
            var result = exceptionContext.Result as ObjectResult;
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.NotNull(result);

            var errorResponse = result.Value as ErrorResponse;
            Assert.NotNull(errorResponse);
            Assert.Equal("Test exception", errorResponse.Message);
        }

        [Fact]
        public async Task ExceptionDataWrapper_HasHandledError()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new ResponseWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new BusinessException("MiCake"), true);
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLoggerFactory.Instance.CreateLogger<ExceptionResponseWrapperFilter>());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.Null(result);
        }

        private static ExceptionContext GetExceptionContext(HttpContext httpContext, Exception exception, bool isHandled = false)
        {
            return new ExceptionContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>())
            {
                ExceptionHandled = isHandled,
                Exception = exception
            };
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
