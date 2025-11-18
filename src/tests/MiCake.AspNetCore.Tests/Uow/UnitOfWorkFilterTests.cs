using MiCake.AspNetCore.Uow;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
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
                UnitOfWork = new MiCakeAspNetUowOption
                {
                    IsAutoUowEnabled = true,
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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAutoTransactionDisabled_ShouldNotBeginUow()
        {
            // Arrange
            var options = new MiCakeAspNetOptions
            {
                UnitOfWork = new MiCakeAspNetUowOption
                {
                    IsAutoUowEnabled = false
                }
            };
            _mockOptions.Setup(o => o.Value).Returns(options);

            var filter = new UnitOfWorkFilter(_mockUowManager.Object, _mockOptions.Object, _mockLogger.Object);
            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<bool>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
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
            _mockUowManager.Verify(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), It.IsAny<bool>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        #endregion

        #region Attribute Configuration Tests

        [Fact]
        public async Task OnActionExecutionAsync_WithUnitOfWorkAttribute_ShouldUseAttributeConfiguration()
        {
            // Arrange
            var attribute = new UnitOfWorkAttribute
            {
                InitializationMode = TransactionInitializationMode.Immediate,
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
                    o.InitializationMode == TransactionInitializationMode.Immediate &&
                    o.IsolationLevel == IsolationLevel.Serializable), 
                false, default))
                .ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            _mockUowManager.Verify(m => m.BeginAsync(
                It.Is<UnitOfWorkOptions>(o => 
                    o.InitializationMode == TransactionInitializationMode.Immediate &&
                    o.IsolationLevel == IsolationLevel.Serializable),
                false,
                default), Times.Once);
        }

        #endregion

        #region Read-Only Detection Tests

        [Theory]
        [InlineData("GetOrders")]
        [InlineData("FindProduct")]
        [InlineData("QueryCustomers")]
        [InlineData("SearchUsers")]
        public async Task OnActionExecutionAsync_WithReadOnlyActionName_ShouldSkipCommit(string actionName)
        {
            // Arrange
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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Never);
            mockUow.Verify(u => u.MarkAsCompletedAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithWriteActionName_ShouldCommit()
        {
            // Arrange
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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            ActionExecutionDelegate next = () => Task.FromResult(executedContext);

            // Act
            await _filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            mockUow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        #endregion

        #region Auto-Commit Tests

        [Fact]
        public async Task OnActionExecutionAsync_OnSuccess_ShouldCommitUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);

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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.RollbackAsync(default)).Returns(Task.CompletedTask);

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

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task OnActionExecutionAsync_ShouldDisposeUow()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.Dispose());

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act
            await _filter.OnActionExecutionAsync(_executingContext, next);

            // Assert
            mockUow.Verify(u => u.Dispose(), Times.Once);
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
            _mockUowManager.Setup(m => m.BeginAsync(It.IsAny<UnitOfWorkOptions>(), false, default)).ReturnsAsync(mockUow.Object);
            mockUow.Setup(u => u.CommitAsync(default))
                .ThrowsAsync(new InvalidOperationException("Commit failed"));

            ActionExecutionDelegate next = () => Task.FromResult(_executedContext);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _filter.OnActionExecutionAsync(_executingContext, next));
        }

        #endregion
    }
}
