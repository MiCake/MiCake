﻿using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
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
                DataWrapperOptions = new DataWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new SoftMiCakeException("MiCake"));
            var wrapperFilter = new ExceptionDataWrapper(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.NotNull(result);
            Assert.IsAssignableFrom<ApiResponse>(result.Value);
        }

        [Fact]
        public async Task ExceptionDataWrapper_NormalException()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new MiCakeException("MiCake"));
            var wrapperFilter = new ExceptionDataWrapper(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.False(exceptionContext.ExceptionHandled);
            Assert.NotNull(result);
            Assert.IsAssignableFrom<ApiError>(result.Value);
        }

        [Fact]
        public async Task ExceptionDataWrapper_NotWrapStatueCode()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
                {
                    NoWrapStatusCode = new List<int>() { 500 }
                }
            });
            var exceptionContext = GetExceptionContext(httpContext, new SoftMiCakeException("MiCake"));
            var wrapperFilter = new ExceptionDataWrapper(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.False(exceptionContext.ExceptionHandled);
            Assert.Null(result);
        }

        [Fact]
        public async Task ExceptionDataWrapper_HasHandledError()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 500);
            var options = Options.Create(new MiCakeAspNetOptions()
            {
                DataWrapperOptions = new DataWrapperOptions()
            });
            var exceptionContext = GetExceptionContext(httpContext, new SoftMiCakeException("MiCake"), true);
            var wrapperFilter = new ExceptionDataWrapper(options, new DefaultWrapperExecutor());

            //action
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            //assert
            var result = exceptionContext.Result as ObjectResult;
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.Null(result);
        }

        private ExceptionContext GetExceptionContext(HttpContext httpContext, Exception exception, bool isHandled = false)
        {
            return new ExceptionContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>())
            {
                ExceptionHandled = isHandled,
                Exception = exception
            };
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