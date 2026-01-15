using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="ApiLoggingFilter"/> implementation.
    /// </summary>
    public class ApiLoggingFilter_Tests
    {
        #region IOrderedFilter Tests

        [Fact]
        public void Order_ShouldReturnIntMaxValue()
        {
            // Arrange
            var filter = CreateFilter();

            // Assert - Filter should run last
            Assert.Equal(int.MaxValue, filter.Order);
        }

        [Fact]
        public void Filter_ShouldImplementIOrderedFilter()
        {
            // Arrange
            var filter = CreateFilter();

            // Assert
            Assert.IsAssignableFrom<IOrderedFilter>(filter);
        }

        [Fact]
        public void Filter_ShouldImplementIAsyncResourceFilter()
        {
            // Arrange
            var filter = CreateFilter();

            // Assert
            Assert.IsAssignableFrom<IAsyncResourceFilter>(filter);
        }

        [Fact]
        public void Filter_ShouldImplementIAsyncResultFilter()
        {
            // Arrange
            var filter = CreateFilter();

            // Assert
            Assert.IsAssignableFrom<IAsyncResultFilter>(filter);
        }

        #endregion

        #region Logging Enabled/Disabled Tests

        [Fact]
        public async Task OnResourceExecutionAsync_LoggingDisabled_SkipsLogging()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(enabled: false),
                logWriter: logWriter);

            var context = CreateResourceExecutingContext();

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert - no log entry should be stored in HttpContext
            Assert.False(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        [Fact]
        public async Task OnResourceExecutionAsync_LoggingEnabled_CreatesLogEntry()
        {
            // Arrange
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(enabled: true));

            var context = CreateResourceExecutingContext();

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.True(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        #endregion

        #region Path Exclusion Tests

        [Fact]
        public async Task OnResourceExecutionAsync_ExcludedPath_SkipsLogging()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludedPaths: ["/health"]),
                logWriter: logWriter);

            var context = CreateResourceExecutingContext(path: "/health");

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.False(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        [Fact]
        public async Task OnResourceExecutionAsync_ExcludedPathWithGlob_SkipsLogging()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludedPaths: ["/api/files/*"]),
                logWriter: logWriter);

            var context = CreateResourceExecutingContext(path: "/api/files/download");

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.False(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        [Fact]
        public async Task OnResourceExecutionAsync_NonExcludedPath_ProcessesLogging()
        {
            // Arrange
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludedPaths: ["/health"]));

            var context = CreateResourceExecutingContext(path: "/api/users");

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.True(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        #endregion

        #region Status Code Exclusion Tests

        [Fact]
        public async Task OnResultExecutionAsync_ExcludedStatusCode_SkipsWriting()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludeStatusCodes: [200]),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext(statusCode: 200);
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext, statusCode: 200);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Empty(logWriter.WrittenEntries);
        }

        [Fact]
        public async Task OnResultExecutionAsync_NonExcludedStatusCode_WritesLog()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludeStatusCodes: [200]),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext(statusCode: 400);
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext, statusCode: 400);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Single(logWriter.WrittenEntries);
        }

        [Fact]
        public async Task OnResultExecutionAsync_EmptyExcludeStatusCodes_LogsAllStatusCodes()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludeStatusCodes: []),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext(statusCode: 200);
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext, statusCode: 200);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Single(logWriter.WrittenEntries);
        }

        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(400)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task OnResultExecutionAsync_EmptyExcludeStatusCodes_LogsAnyStatusCode(int statusCode)
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludeStatusCodes: []),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext(statusCode: statusCode);
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext, statusCode: statusCode);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Single(logWriter.WrittenEntries);
            Assert.Equal(statusCode, logWriter.WrittenEntries[0].Response.StatusCode);
        }

        #endregion

        #region Attribute Tests

        [Fact]
        public async Task OnResourceExecutionAsync_SkipApiLoggingAttribute_SkipsLogging()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(logWriter: logWriter);

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipLoggingController),
                nameof(SkipLoggingController.SkippedAction));

            var context = CreateResourceExecutingContext(actionDescriptor: actionDescriptor);

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.False(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        [Fact]
        public async Task OnResourceExecutionAsync_SkipApiLoggingAttributeOnController_SkipsLogging()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(logWriter: logWriter);

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(SkipLoggingAtControllerLevelController),
                nameof(SkipLoggingAtControllerLevelController.SomeAction));

            var context = CreateResourceExecutingContext(actionDescriptor: actionDescriptor);

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.False(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
        }

        [Fact]
        public async Task OnResultExecutionAsync_AlwaysLogAttribute_IgnoresStatusCodeExclusion()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(excludeStatusCodes: [200]),
                logWriter: logWriter);

            var actionDescriptor = CreateControllerActionDescriptor(
                typeof(AlwaysLogController),
                nameof(AlwaysLogController.AlwaysLoggedAction));

            var resourceContext = CreateResourceExecutingContext(statusCode: 200, actionDescriptor: actionDescriptor);
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext, statusCode: 200);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert - Even though 200 is excluded, AlwaysLog attribute forces logging
            Assert.Single(logWriter.WrittenEntries);
        }

        #endregion

        #region Processor Pipeline Tests

        [Fact]
        public async Task OnResultExecutionAsync_ProcessorsPipelineExecuted_InOrder()
        {
            // Arrange
            var orderTracker = new List<int>();
            var processor1 = new OrderTrackingProcessor(orderTracker, 10);
            var processor2 = new OrderTrackingProcessor(orderTracker, 5);
            var processor3 = new OrderTrackingProcessor(orderTracker, 20);

            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                logWriter: logWriter,
                processors: [processor1, processor2, processor3]);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert - Processors should be executed in order of their Order property
            Assert.Equal([5, 10, 20], orderTracker);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ProcessorReturnsNull_StopsPipeline()
        {
            // Arrange
            var nullProcessor = new NullReturningProcessor();
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                logWriter: logWriter,
                processors: [nullProcessor]);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert - No log should be written when processor returns null
            Assert.Empty(logWriter.WrittenEntries);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ProcessorThrowsException_ContinuesWithNextProcessor()
        {
            // Arrange
            var throwingProcessor = new ThrowingProcessor();
            var normalProcessor = new PassThroughProcessor();
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                logWriter: logWriter,
                processors: [throwingProcessor, normalProcessor]);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert - Log should still be written despite processor exception
            Assert.Single(logWriter.WrittenEntries);
        }

        #endregion

        #region Request Body Capture Tests

        [Fact]
        public async Task OnResourceExecutionAsync_LogRequestBodyEnabled_CapturesBody()
        {
            // Arrange
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(logRequestBody: true));

            var requestBody = """{"name": "test"}""";
            var context = CreateResourceExecutingContext(requestBody: requestBody);

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.True(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
            var entry = context.HttpContext.Items["MiCake.ApiLogging.Entry"] as ApiLogEntry;
            Assert.NotNull(entry);
            Assert.Equal(requestBody, entry.Request.Body);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_LogRequestBodyDisabled_DoesNotCaptureBody()
        {
            // Arrange
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(logRequestBody: false));

            var requestBody = """{"name": "test"}""";
            var context = CreateResourceExecutingContext(requestBody: requestBody);

            // Act
            await filter.OnResourceExecutionAsync(context, () => Task.FromResult(CreateResourceExecutedContext(context)));

            // Assert
            Assert.True(context.HttpContext.Items.ContainsKey("MiCake.ApiLogging.Entry"));
            var entry = context.HttpContext.Items["MiCake.ApiLogging.Entry"] as ApiLogEntry;
            Assert.NotNull(entry);
            Assert.Null(entry.Request.Body);
        }

        #endregion

        #region Response Body Capture Tests

        [Fact]
        public async Task OnResultExecutionAsync_ObjectResult_CapturesSerializedBody()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(logResponseBody: true),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(
                resourceContext.HttpContext,
                result: new ObjectResult(new { Id = 1, Name = "Test" }));

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Single(logWriter.WrittenEntries);
            Assert.Contains("Id", logWriter.WrittenEntries[0].Response.Body);
            Assert.Contains("Test", logWriter.WrittenEntries[0].Response.Body);
        }

        [Fact]
        public async Task OnResultExecutionAsync_LogResponseBodyDisabled_DoesNotCaptureBody()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(
                configProvider: new TestConfigProvider(logResponseBody: false),
                logWriter: logWriter);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(
                resourceContext.HttpContext,
                result: new ObjectResult(new { Id = 1, Name = "Test" }));

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Single(logWriter.WrittenEntries);
            Assert.Null(logWriter.WrittenEntries[0].Response.Body);
        }

        #endregion

        #region No Log Entry Tests

        [Fact]
        public async Task OnResultExecutionAsync_NoLogEntry_SkipsProcessing()
        {
            // Arrange
            var logWriter = new TestApiLogWriter();
            var filter = CreateFilter(logWriter: logWriter);

            // Don't call OnActionExecutionAsync first - no log entry exists
            var httpContext = CreateFakeHttpContext("GET", "/api/test", 200);
            var resultContext = CreateResultExecutingContext(httpContext);

            // Act
            await filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext)));

            // Assert
            Assert.Empty(logWriter.WrittenEntries);
        }

        #endregion

        #region LogWriter Exception Handling Tests

        [Fact]
        public async Task OnResultExecutionAsync_LogWriterThrowsException_DoesNotPropagateException()
        {
            // Arrange
            var throwingLogWriter = new ThrowingApiLogWriter();
            var filter = CreateFilter(logWriter: throwingLogWriter);

            var resourceContext = CreateResourceExecutingContext();
            await filter.OnResourceExecutionAsync(resourceContext, () => Task.FromResult(CreateResourceExecutedContext(resourceContext)));

            var resultContext = CreateResultExecutingContext(resourceContext.HttpContext);

            // Act & Assert - Should not throw
            var exception = await Record.ExceptionAsync(() =>
                filter.OnResultExecutionAsync(resultContext, () => Task.FromResult(CreateResultExecutedContext(resultContext))));

            Assert.Null(exception);
        }

        #endregion

        #region Helper Methods

        private static ApiLoggingFilter CreateFilter(
            IApiLoggingConfigProvider? configProvider = null,
            IApiLogWriter? logWriter = null,
            IEnumerable<IApiLogProcessor>? processors = null)
        {
            return new ApiLoggingFilter(
                configProvider ?? new TestConfigProvider(),
                new TestApiLogEntryFactory(),
                processors ?? Array.Empty<IApiLogProcessor>(),
                logWriter ?? new TestApiLogWriter(),
                NullLogger<ApiLoggingFilter>.Instance);
        }

        private static ResourceExecutingContext CreateResourceExecutingContext(
            string method = "GET",
            string path = "/api/test",
            int statusCode = 200,
            string? requestBody = null,
            ActionDescriptor? actionDescriptor = null)
        {
            var httpContext = CreateFakeHttpContext(method, path, statusCode, requestBody);

            return new ResourceExecutingContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(),
                    actionDescriptor ?? new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new List<IValueProviderFactory>());
        }

        private static ResourceExecutedContext CreateResourceExecutedContext(ResourceExecutingContext context)
        {
            return new ResourceExecutedContext(
                context,
                context.Filters);
        }

        private static ResultExecutingContext CreateResultExecutingContext(
            HttpContext httpContext,
            int statusCode = 200,
            IActionResult? result = null)
        {
            httpContext.Response.StatusCode = statusCode;

            return new ResultExecutingContext(
                new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                result ?? new OkResult(),
                new object());
        }

        private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
        {
            return new ResultExecutedContext(
                context,
                context.Filters,
                context.Result,
                context.Controller);
        }

        private static ControllerActionDescriptor CreateControllerActionDescriptor(
            Type controllerType,
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

        private static HttpContext CreateFakeHttpContext(
            string method,
            string path,
            int statusCode,
            string? requestBody = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;
            httpContext.Response.StatusCode = statusCode;

            if (!string.IsNullOrEmpty(requestBody))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                httpContext.Request.Body = new MemoryStream(bodyBytes);
                httpContext.Request.ContentLength = bodyBytes.Length;
                httpContext.Request.ContentType = "application/json";
            }

            return httpContext;
        }

        #endregion

        #region Test Controllers

        /// <summary>
        /// Controller with action-level SkipApiLogging attribute
        /// </summary>
        public class SkipLoggingController : ControllerBase
        {
            [SkipApiLogging]
            public IActionResult SkippedAction() => Ok();

            public IActionResult NormalAction() => Ok();
        }

        /// <summary>
        /// Controller with controller-level SkipApiLogging attribute
        /// </summary>
        [SkipApiLogging]
        public class SkipLoggingAtControllerLevelController : ControllerBase
        {
            public IActionResult SomeAction() => Ok();
        }

        /// <summary>
        /// Controller with AlwaysLog attribute
        /// </summary>
        public class AlwaysLogController : ControllerBase
        {
            [AlwaysLog]
            public IActionResult AlwaysLoggedAction() => Ok();
        }

        /// <summary>
        /// Controller with LogFullResponse attribute
        /// </summary>
        public class LogFullResponseController : ControllerBase
        {
            [LogFullResponse]
            public IActionResult FullLogAction() => Ok();

            [LogFullResponse(MaxSize = 10000)]
            public IActionResult FullLogWithSizeAction() => Ok();
        }

        #endregion

        #region Test Implementations

        private class TestConfigProvider : IApiLoggingConfigProvider
        {
            private readonly ApiLoggingEffectiveConfig _config;

            public TestConfigProvider(
                bool enabled = true,
                List<int>? excludeStatusCodes = null,
                List<string>? excludedPaths = null,
                bool logRequestBody = true,
                bool logResponseBody = true)
            {
                _config = new ApiLoggingEffectiveConfig
                {
                    Enabled = enabled,
                    ExcludeStatusCodes = excludeStatusCodes ?? [],
                    ExcludedPaths = excludedPaths ?? [],
                    LogRequestBody = logRequestBody,
                    LogResponseBody = logResponseBody,
                    MaxRequestBodySize = 4096,
                    MaxResponseBodySize = 4096,
                    SensitiveFields = ["password", "token"],
                    ExcludedContentTypes = []
                };
            }

            public Task<ApiLoggingEffectiveConfig> GetEffectiveConfigAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(_config);

            public Task RefreshAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private class TestApiLogWriter : IApiLogWriter
        {
            public List<ApiLogEntry> WrittenEntries { get; } = [];

            public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
            {
                WrittenEntries.Add(entry);
                return Task.CompletedTask;
            }
        }

        private class ThrowingApiLogWriter : IApiLogWriter
        {
            public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Test exception");
            }
        }

        private class TestApiLogEntryFactory : IApiLogEntryFactory
        {
            public ApiLogEntry CreateEntry(HttpContext httpContext, ApiLoggingEffectiveConfig configuration)
            {
                return new ApiLogEntry
                {
                    CorrelationId = httpContext.TraceIdentifier,
                    Timestamp = DateTimeOffset.UtcNow,
                    Request = new ApiRequestLog
                    {
                        Method = httpContext.Request.Method,
                        Path = httpContext.Request.Path.Value ?? string.Empty,
                        QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null
                    }
                };
            }

            public void PopulateResponse(ApiLogEntry entry, HttpContext httpContext, ApiLoggingEffectiveConfig configuration, string? responseBody, TimeSpan elapsed)
            {
                entry.Response = new ApiResponseLog
                {
                    StatusCode = httpContext.Response.StatusCode,
                    Body = responseBody
                };
                entry.ElapsedMilliseconds = (long)elapsed.TotalMilliseconds;
            }
        }

        private class OrderTrackingProcessor : IApiLogProcessor
        {
            private readonly List<int> _orderTracker;
            private readonly int _order;

            public int Order => _order;

            public OrderTrackingProcessor(List<int> orderTracker, int order)
            {
                _orderTracker = orderTracker;
                _order = order;
            }

            public Task<ApiLogEntry?> ProcessAsync(ApiLogEntry entry, ApiLogProcessingContext context, CancellationToken cancellationToken = default)
            {
                _orderTracker.Add(_order);
                return Task.FromResult<ApiLogEntry?>(entry);
            }
        }

        private class NullReturningProcessor : IApiLogProcessor
        {
            public int Order => 0;

            public Task<ApiLogEntry?> ProcessAsync(ApiLogEntry entry, ApiLogProcessingContext context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ApiLogEntry?>(null);
            }
        }

        private class ThrowingProcessor : IApiLogProcessor
        {
            public int Order => 0;

            public Task<ApiLogEntry?> ProcessAsync(ApiLogEntry entry, ApiLogProcessingContext context, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Test processor exception");
            }
        }

        private class PassThroughProcessor : IApiLogProcessor
        {
            public int Order => 100;

            public Task<ApiLogEntry?> ProcessAsync(ApiLogEntry entry, ApiLogProcessingContext context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ApiLogEntry?>(entry);
            }
        }

        #endregion
    }
}
