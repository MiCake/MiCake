using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class SkipResponseWrapperAttribute_Tests
    {
        #region ResponseWrapperFilter Tests

        [Fact]
        public async Task ResponseWrapperFilter_WithSkipAttribute_DoesNotWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ObjectResult objectResult = new(new { Data = "test" });

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipWrappingController),
                nameof(SkipWrappingController.GetDataWithSkip));

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult, actionDescriptor);
            var wrapperFilter = new ResponseWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should not wrap because of SkipResponseWrapperAttribute
            Assert.NotNull(resultExecutingContext.Result);
            var resultValue = ((ObjectResult)resultExecutingContext.Result).Value;
            Assert.False(resultValue is IResponseWrapper, "Response should not be wrapped when SkipResponseWrapperAttribute is present");
        }

        [Fact]
        public async Task ResponseWrapperFilter_WithControllerLevelSkipAttribute_DoesNotWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ObjectResult objectResult = new(new { Data = "test" });

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipWrappingAtControllerLevelController),
                nameof(SkipWrappingAtControllerLevelController.GetData));

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult, actionDescriptor);
            var wrapperFilter = new ResponseWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should not wrap because controller has SkipResponseWrapperAttribute
            Assert.NotNull(resultExecutingContext.Result);
            var resultValue = ((ObjectResult)resultExecutingContext.Result).Value;
            Assert.False(resultValue is IResponseWrapper, "Response should not be wrapped when controller has SkipResponseWrapperAttribute");
        }

        [Fact]
        public async Task ResponseWrapperFilter_WithoutSkipAttribute_WrapsNormally()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            ObjectResult objectResult = new(new { Data = "test" });

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(NormalController),
                nameof(NormalController.GetData));

            var resultExecutingContext = GetResourceExecutingContext(httpContext, objectResult, actionDescriptor);
            var wrapperFilter = new ResponseWrapperFilter(options);

            // Act
            await wrapperFilter.OnResultExecutionAsync(resultExecutingContext,
                                                       () => Task.FromResult(CreateResultExecutedContext(resultExecutingContext)));

            // Assert - should wrap normally
            Assert.NotNull(resultExecutingContext.Result);
            var resultValue = ((ObjectResult)resultExecutingContext.Result).Value;
            Assert.True(resultValue is IResponseWrapper, "Response should be wrapped when no SkipResponseWrapperAttribute is present");
        }

        #endregion

        #region ExceptionResponseWrapperFilter Tests

        [Fact]
        public async Task ExceptionWrapperFilter_WithSkipAttribute_DoesNotWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var exception = new System.Exception("Test exception");

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipWrappingController),
                nameof(SkipWrappingController.GetDataWithSkip));

            var exceptionContext = CreateExceptionContext(httpContext, exception, actionDescriptor);
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLogger<ExceptionResponseWrapperFilter>.Instance);

            // Act
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            // Assert - should not wrap because of SkipResponseWrapperAttribute
            Assert.False(exceptionContext.ExceptionHandled, "Exception should not be handled when SkipResponseWrapperAttribute is present");
            Assert.Null(exceptionContext.Result);
        }

        [Fact]
        public async Task ExceptionWrapperFilter_WithControllerLevelSkipAttribute_DoesNotWrap()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var exception = new System.Exception("Test exception");

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipWrappingAtControllerLevelController),
                nameof(SkipWrappingAtControllerLevelController.GetData));

            var exceptionContext = CreateExceptionContext(httpContext, exception, actionDescriptor);
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLogger<ExceptionResponseWrapperFilter>.Instance);

            // Act
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            // Assert - should not wrap because controller has SkipResponseWrapperAttribute
            Assert.False(exceptionContext.ExceptionHandled, "Exception should not be handled when controller has SkipResponseWrapperAttribute");
            Assert.Null(exceptionContext.Result);
        }

        [Fact]
        public async Task ExceptionWrapperFilter_WithoutSkipAttribute_WrapsNormally()
        {
            // Arrange
            var httpContext = CreateFakeHttpContext("Get", 200);
            var exception = new System.Exception("Test exception");

            var options = Options.Create(new MiCakeAspNetOptions()
            {
                UseDataWrapper = true,
                DataWrapperOptions = new ResponseWrapperOptions()
            });

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(NormalController),
                nameof(NormalController.GetData));

            var exceptionContext = CreateExceptionContext(httpContext, exception, actionDescriptor);
            var wrapperFilter = new ExceptionResponseWrapperFilter(options, NullLogger<ExceptionResponseWrapperFilter>.Instance);

            // Act
            await wrapperFilter.OnExceptionAsync(exceptionContext);

            // Assert - should wrap normally
            Assert.True(exceptionContext.ExceptionHandled, "Exception should be handled when no SkipResponseWrapperAttribute is present");
            Assert.NotNull(exceptionContext.Result);
            var resultValue = ((ObjectResult)exceptionContext.Result).Value;
            Assert.True(resultValue is IResponseWrapper, "Response should be wrapped when no SkipResponseWrapperAttribute is present");
        }

        #endregion

        #region Attribute Functionality Tests

        [Fact]
        public void SkipResponseWrapperAttribute_CanBeAppliedToClass()
        {
            // Arrange & Act
            var attribute = typeof(SkipWrappingAtControllerLevelController)
                .GetCustomAttribute<SkipResponseWrapperAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void SkipResponseWrapperAttribute_CanBeAppliedToMethod()
        {
            // Arrange & Act
            var method = typeof(SkipWrappingController).GetMethod(nameof(SkipWrappingController.GetDataWithSkip));
            var attribute = method?.GetCustomAttribute<SkipResponseWrapperAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void SkipResponseWrapperAttribute_IsNotPresent_OnNormalMethods()
        {
            // Arrange & Act
            var method = typeof(NormalController).GetMethod(nameof(NormalController.GetData));
            var attribute = method?.GetCustomAttribute<SkipResponseWrapperAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void SkipResponseWrapperAttribute_InheritedFromBaseController()
        {
            // Arrange & Act
            var attribute = typeof(DerivedSkipWrappingController)
                .GetCustomAttribute<SkipResponseWrapperAttribute>(inherit: true);

            // Assert
            Assert.NotNull(attribute);
        }

        #endregion

        #region Helper Methods

        private static ResultExecutingContext GetResourceExecutingContext(
            HttpContext httpContext,
            IActionResult actionResult,
            ActionDescriptor? actionDescriptor = null)
        {
            return new ResultExecutingContext(
                new ActionContext(
                    httpContext,
                    new Microsoft.AspNetCore.Routing.RouteData(),
                    actionDescriptor ?? new ActionDescriptor()),
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

        private static ExceptionContext CreateExceptionContext(
            HttpContext httpContext,
            System.Exception exception,
            ActionDescriptor? actionDescriptor = null)
        {
            return new ExceptionContext(
                new ActionContext(
                    httpContext,
                    new Microsoft.AspNetCore.Routing.RouteData(),
                    actionDescriptor ?? new ActionDescriptor()),
                new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        private static ControllerActionDescriptor CreateControllerActionDescriptor(
            System.Type controllerType,
            string actionName)
        {
            var methodInfo = controllerType.GetMethod(actionName);
            return new ControllerActionDescriptor
            {
                ControllerTypeInfo = controllerType.GetTypeInfo(),
                MethodInfo = methodInfo!,
                ActionName = actionName
            };
        }

        private static HttpContext CreateFakeHttpContext(string method, int statusCode)
        {
            var fakeHttpContext = new DefaultHttpContext();
            fakeHttpContext.Request.Method = method;
            fakeHttpContext.Response.StatusCode = statusCode;

            return fakeHttpContext;
        }

        #endregion

        #region Test Controllers

        /// <summary>
        /// Controller with action-level SkipResponseWrapper attribute
        /// </summary>
        public class SkipWrappingController : ControllerBase
        {
            [SkipResponseWrapper]
            public IActionResult GetDataWithSkip()
            {
                return Ok(new { Data = "test" });
            }

            public IActionResult GetDataWithoutSkip()
            {
                return Ok(new { Data = "test" });
            }
        }

        /// <summary>
        /// Controller with controller-level SkipResponseWrapper attribute
        /// </summary>
        [SkipResponseWrapper]
        public class SkipWrappingAtControllerLevelController : ControllerBase
        {
            public IActionResult GetData()
            {
                return Ok(new { Data = "test" });
            }
        }

        /// <summary>
        /// Controller without SkipResponseWrapper attribute
        /// </summary>
        public class NormalController : ControllerBase
        {
            public IActionResult GetData()
            {
                return Ok(new { Data = "test" });
            }
        }

        /// <summary>
        /// Base controller with SkipResponseWrapper attribute
        /// </summary>
        [SkipResponseWrapper]
        public class BaseSkipWrappingController : ControllerBase
        {
        }

        /// <summary>
        /// Derived controller that inherits SkipResponseWrapper from base
        /// </summary>
        public class DerivedSkipWrappingController : BaseSkipWrappingController
        {
            public IActionResult GetData()
            {
                return Ok(new { Data = "test" });
            }
        }

        #endregion
    }
}
