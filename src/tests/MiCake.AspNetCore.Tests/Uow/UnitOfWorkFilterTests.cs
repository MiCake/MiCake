using MiCake.AspNetCore.Uow;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for UnitOfWorkFilter
    /// Tests auto-begin, auto-commit, auto-rollback, and attribute-based configuration
    /// </summary>
    public class UnitOfWorkFilterTests
    {
        private static readonly MethodInfo TestMethodInfo = typeof(object).GetMethod("ToString")!;
        private readonly Mock<IUnitOfWorkManager> _mockUowManager;
        private readonly Mock<ILogger<UnitOfWorkFilter>> _mockLogger;
        private readonly Mock<IOptions<MiCakeAspNetOptions>> _mockOptions;
        private readonly UnitOfWorkFilter _filter;
        private readonly ActionExecutingContext _executingContext;
        private readonly ActionExecutedContext _executedContext;
        private readonly ActionContext _actionContext;

        public UnitOfWorkFilterTests()
        {
            _mockUowManager = new Mock<IUnitOfWorkManager>();
            _mockLogger = new Mock<ILogger<UnitOfWorkFilter>>();
            _mockOptions = new Mock<IOptions<MiCakeAspNetOptions>>();

            var aspNetOptions = new MiCakeAspNetOptions
            {
                UnitOfWork = new MiCakeAspNetUowOptions
                {
                    EnableAutoUnitOfWork = true,
                    EnableReadOnlyActionNameInference = false,
                    ReadOnlyActionKeywords = new List<string> { "Get", "Find", "Query", "Search" }
                }
            };
            _mockOptions.Setup(o => o.Value).Returns(aspNetOptions);

            _filter = new UnitOfWorkFilter(_mockUowManager.Object, _mockOptions.Object, _mockLogger.Object);

            // Setup contexts
            var httpContext = new DefaultHttpContext();
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo()
            };

            _actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                controllerActionDescriptor
            );

            _executingContext = new ActionExecutingContext(
                _actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            _executedContext = new ActionExecutedContext(
                _actionContext,
                new List<IFilterMetadata>(),
                new object()
            );
        }

        #region Auto-Begin Tests

        [Fact]
        public async Task OnActionExecutionAsync_WithAutoTransactionEnabled_ShouldBeginUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAutoTransactionDisabled_ShouldNotBeginUow()
        {
            // Arrange
            var options = new MiCakeAspNetOptions
            {
                UnitOfWork = new MiCakeAspNetUowOptions
                {
                    EnableAutoUnitOfWork = false
                }
            };
            _mockOptions.Setup(o => o.Value).Returns(options);

            var filter = new UnitOfWorkFilter(_mockUowManager.Object, _mockOptions.Object, _mockLogger.Object);
            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithDisableUnitOfWorkAttribute_ShouldNotBeginUow()
        {
            // Arrange
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                EndpointMetadata = new List<object> { new DisableUnitOfWorkAttribute() }
            };
            
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        #endregion

        #region Attribute Configuration Tests

        [Fact]
        public async Task OnActionExecutionAsync_WithUnitOfWorkAttribute_ShouldUseAttributeConfiguration()
        {
            // Arrange
            var attribute = new UnitOfWorkAttribute
            {
                IsReadOnly = true,
                IsolationLevel = IsolationLevel.Serializable
            };

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                EndpointMetadata = new List<object> { attribute }
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o =>
                    o.IsReadOnly &&
                    o.IsolationLevel == IsolationLevel.Serializable),
                default))
                .ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o =>
                    o.IsReadOnly &&
                    o.IsolationLevel == IsolationLevel.Serializable),
                default), Times.Once);

            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithUnitOfWorkAttributeWithoutIsolationLevel_ShouldUseUowDefault()
        {
            // Arrange
            var attribute = new UnitOfWorkAttribute
            {
                IsReadOnly = false
            };

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                EndpointMetadata = new List<object> { attribute }
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => !o.IsReadOnly && o.IsolationLevel == IsolationLevel.ReadCommitted),
                default))
                .ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => !o.IsReadOnly && o.IsolationLevel == IsolationLevel.ReadCommitted),
                default), Times.Once);

            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        #endregion

        #region Read-Only Detection Tests

        [Theory]
        [InlineData("GetOrders")]
        [InlineData("FindProduct")]
        [InlineData("QueryCustomers")]
        [InlineData("SearchUsers")]
        public async Task OnActionExecutionAsync_WithReadOnlyActionNameAndInferenceEnabled_ShouldSkipCommit(string actionName)
        {
            // Arrange
            var filter = CreateFilterWithInferenceEnabled();

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = actionName,
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = actionName
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithReadOnlyActionNameAndInferenceDisabled_ShouldCommit()
        {
            // Arrange
            // Action-name inference is opt-in; the default configuration (inference disabled)
            // must treat Get-prefixed actions as writable.
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "GetOrders",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "GetOrders"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithWriteActionNameAndInferenceEnabled_ShouldCommit()
        {
            // Arrange
            var filter = CreateFilterWithInferenceEnabled();

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "CreateOrder",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "CreateOrder"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithExplicitReadOnlyAttribute_ShouldSkipCommitEvenWhenInferenceDisabled()
        {
            // Arrange
            // Explicit metadata wins even when action-name inference is disabled.
            var attribute = new UnitOfWorkAttribute
            {
                IsReadOnly = true
            };

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "GetOrders",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "GetOrders",
                EndpointMetadata = new List<object> { attribute }
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => o.IsReadOnly),
                default))
                .ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithExplicitWritableAttribute_ShouldCommitEvenWhenInferenceEnabled()
        {
            // Arrange
            var filter = CreateFilterWithInferenceEnabled();

            // Explicit IsReadOnly = false overrides the read-only name inference.
            var attribute = new UnitOfWorkAttribute
            {
                IsReadOnly = false
            };

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "GetOrders",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "GetOrders",
                EndpointMetadata = new List<object> { attribute }
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => !o.IsReadOnly),
                default))
                .ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithControllerLevelReadOnlyAttribute_ShouldSkipCommit()
        {
            // Arrange
            // Attribute lookup resolves the controller type before endpoint metadata.
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "GetOrders",
                ControllerName = "ReadOnlyMarker",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(ReadOnlyMarkerController).GetTypeInfo(),
                DisplayName = "GetOrders"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => o.IsReadOnly),
                default))
                .ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithMethodLevelReadOnlyAttribute_ShouldSkipCommit()
        {
            // Arrange
            // Attribute lookup resolves the action method before controller type and endpoint metadata.
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "ReadOnlyMarkedAction",
                ControllerName = "TestController",
                MethodInfo = typeof(AttributeMarkers).GetMethod(nameof(AttributeMarkers.ReadOnlyMarkedAction))!,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "ReadOnlyMarkedAction"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => o.IsReadOnly),
                default))
                .ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithControllerLevelDisableAttribute_ShouldNotBeginUow()
        {
            // Arrange
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "AnyAction",
                ControllerName = "DisableMarker",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(DisableMarkerController).GetTypeInfo()
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithMethodLevelDisableAttribute_ShouldNotBeginUow()
        {
            // Arrange
            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "DisableMarkedAction",
                ControllerName = "TestController",
                MethodInfo = typeof(AttributeMarkers).GetMethod(nameof(AttributeMarkers.DisableMarkedAction))!,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "DisableMarkedAction"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithEmptyReadOnlyKeywords_ShouldCommit()
        {
            // Arrange
            // With an empty keyword list the inference never matches, so Get-prefixed
            // actions remain writable even when inference is enabled.
            var filter = CreateFilterWithInference(new List<string>());

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "GetOrders",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "GetOrders"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithMixedCaseActionNameAndInferenceEnabled_ShouldSkipCommit()
        {
            // Arrange
            // Keyword matching is case-insensitive.
            var filter = CreateFilterWithInferenceEnabled();

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "gEtOrDeRs",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                DisplayName = "gEtOrDeRs"
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        #endregion

        #region Auto-Commit Tests

        [Fact]
        public async Task OnActionExecutionAsync_OnSuccess_ShouldCommitUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_OnException_ShouldRollbackUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var executedContextWithException = new ActionExecutedContext(
                _actionContext,
                new List<IFilterMetadata>(),
                new object()
            );
            executedContextWithException.Exception = new InvalidOperationException("Test exception");

            ActionExecutionDelegate next = () => Task.FromResult(executedContextWithException);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_OnCanceled_ShouldRollbackUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var executedContextCanceled = new ActionExecutedContext(
                _executingContext,
                new List<IFilterMetadata>(),
                new object()
            );
            executedContextCanceled.Canceled = true;
            executedContextCanceled.Exception = new OperationCanceledException();

            ActionExecutionDelegate next = () => Task.FromResult(executedContextCanceled);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_OnCanceledWithoutException_ShouldRollbackUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var executedContextCanceled = new ActionExecutedContext(
                _executingContext,
                new List<IFilterMetadata>(),
                new object()
            );
            executedContextCanceled.Canceled = true;

            ActionExecutionDelegate next = () => Task.FromResult(executedContextCanceled);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.RollbackAsync(default), Times.Once);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task OnActionExecutionAsync_ShouldDisposeUowAsynchronously()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenActionFails_ShouldDisposeUowAsynchronously()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var executedContextWithException = new ActionExecutedContext(
                _actionContext,
                new List<IFilterMetadata>(),
                new object()
            );
            executedContextWithException.Exception = new InvalidOperationException("Test exception");

            ActionExecutionDelegate next = () => Task.FromResult(executedContextWithException);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        #endregion

        #region IOrderedFilter Tests

        [Fact]
        public void Order_ShouldReturnIntMaxValue()
        {
            // Assert - Filter should run last
            Assert.Equal(int.MaxValue, _filter.Order);
        }

        [Fact]
        public void Filter_ShouldImplementIOrderedFilter()
        {
            // Assert
            Assert.IsAssignableFrom<IOrderedFilter>(_filter);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Ctor_WithNullUowManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UnitOfWorkFilter(null, _mockOptions.Object, _mockLogger.Object));
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithCommitFailing_ShouldPropagateException()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default))
                .ThrowsAsync(new InvalidOperationException("Commit failed"));
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _filter.OnActionExecutionAsync(_executingContext, next));

            // The failed commit must leave the UoW rolled back and asynchronously disposed
            mockUow.Verify(u => u.RollbackAsync(default), Times.Once);
            mockUow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenBeginFails_ShouldPropagateExceptionWithoutDisposing()
        {
            // Arrange
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default))
                .ThrowsAsync(new InvalidOperationException("Begin failed"));

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _filter.OnActionExecutionAsync(_executingContext, next));
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenCommitAndRollbackBothFail_ShouldThrowAggregateException()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default))
                .ThrowsAsync(new InvalidOperationException("Commit failed"));
            mockUow.Setup(u => u.RollbackAsync(default))
                .ThrowsAsync(new InvalidOperationException("Rollback failed"));
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AggregateException>(() =>
                _filter.OnActionExecutionAsync(_executingContext, next));

            Assert.Equal(2, ex.InnerExceptions.Count);
            mockUow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenMarkAsCompletedFails_ShouldRollbackAndPropagateException()
        {
            // Arrange
            var attribute = new UnitOfWorkAttribute
            {
                IsReadOnly = true
            };

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "TestController",
                MethodInfo = TestMethodInfo,
                ControllerTypeInfo = typeof(UnitOfWorkFilterTests).GetTypeInfo(),
                EndpointMetadata = new List<object> { attribute }
            };

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                controllerActionDescriptor
            );

            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object()
            );

            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => o.IsReadOnly),
                default))
                .ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.MarkAsCompletedAsync(default))
                .ThrowsAsync(new InvalidOperationException("MarkAsCompleted failed"));
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _filter.OnActionExecutionAsync(executingContext, next));

            // The failed completion must leave the UoW rolled back and asynchronously disposed
            mockUow.Verify(u => u.RollbackAsync(default), Times.Once);
            mockUow.Verify(u => u.DisposeAsync(), Times.Once);
        }

        #endregion

        private UnitOfWorkFilter CreateFilterWithInferenceEnabled()
        {
            return CreateFilterWithInference(ReadOnlyKeywords);
        }

        private UnitOfWorkFilter CreateFilterWithInference(List<string> keywords)
        {
            var options = new MiCakeAspNetOptions
            {
                UnitOfWork = new MiCakeAspNetUowOptions
                {
                    EnableAutoUnitOfWork = true,
                    EnableReadOnlyActionNameInference = true,
                    ReadOnlyActionKeywords = keywords
                }
            };
            _mockOptions.Setup(o => o.Value).Returns(options);
            return new UnitOfWorkFilter(_mockUowManager.Object, _mockOptions.Object, _mockLogger.Object);
        }

        private static readonly List<string> ReadOnlyKeywords = new() { "Get", "Find", "Query", "Search" };

        [UnitOfWork(IsReadOnly = true)]
        public sealed class ReadOnlyMarkerController
        {
        }

        [DisableUnitOfWork]
        public sealed class DisableMarkerController
        {
        }

        /// <summary>
        /// Marker methods carrying attributes for lookup-order tests.
        /// Kept outside the test class to avoid xUnit treating them as test candidates.
        /// </summary>
        public static class AttributeMarkers
        {
            [UnitOfWork(IsReadOnly = true)]
            public static void ReadOnlyMarkedAction()
            {
            }

            [DisableUnitOfWork]
            public static void DisableMarkedAction()
            {
            }
        }
    }
}
