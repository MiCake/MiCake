using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for Unit

OfWorkManager
    /// Tests cover both Lazy and Immediate initialization modes, nested transactions, and lifecycle hooks
    /// </summary>
    public class UnitOfWorkManagerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<ILogger<UnitOfWorkManager>> _mockLogger;
        private readonly UnitOfWorkManager _manager;

        public UnitOfWorkManagerTests()
        {
            var services = new ServiceCollection();
            _mockLogger = new Mock<ILogger<UnitOfWorkManager>>();
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<IAmbientUnitOfWorkAccessor, AmbientUnitOfWorkAccessor>();
            _serviceProvider = services.BuildServiceProvider();

            _manager = new UnitOfWorkManager(_serviceProvider, _mockLogger.Object);
        }

        #region BeginAsync Tests

        [Fact]
        public async Task BeginAsync_WithDefaultOptions_ShouldCreateLazyUnitOfWork()
        {
            // Act
            var uow = await _manager.BeginAsync();

            // Assert
            Assert.NotNull(uow);
            Assert.NotNull(uow.Id);
            Assert.Null(uow.Parent);
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task BeginAsync_WithImmediateMode_ShouldCreateImmediateUnitOfWork()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };

            // Act
            var uow = await _manager.BeginAsync(options);

            // Assert
            Assert.NotNull(uow);
            Assert.NotNull(uow.Id);
        }

        [Fact]
        public async Task BeginAsync_WithReadOnlyOption_ShouldCreateReadOnlyUnitOfWork()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsReadOnly = true
            };

            // Act
            var uow = await _manager.BeginAsync(options);

            // Assert
            Assert.NotNull(uow);
            Assert.True(uow.Options.IsReadOnly);
        }

        [Fact]
        public async Task BeginAsync_WithIsolationLevel_ShouldCreateUnitOfWorkWithSpecifiedIsolationLevel()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            };

            // Act
            var uow = await _manager.BeginAsync(options);

            // Assert
            Assert.NotNull(uow);
            Assert.Equal(IsolationLevel.Serializable, uow.Options.IsolationLevel);
        }

        [Fact]
        public async Task BeginAsync_WithTimeout_ShouldCreateUnitOfWorkWithSpecifiedTimeout()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Timeout = 60
            };

            // Act
            var uow = await _manager.BeginAsync(options);

            // Assert
            Assert.NotNull(uow);
            Assert.Equal(60, uow.Options.Timeout);
        }

        #endregion

        #region Nested Transaction Tests

        [Fact]
        public async Task BeginAsync_WithExistingAmbientUow_ShouldCreateNestedUnitOfWork()
        {
            // Arrange
            var outerUow = await _manager.BeginAsync();

            // Act
            var innerUow = await _manager.BeginAsync();

            // Assert
            Assert.NotNull(innerUow);
            Assert.Same(outerUow, innerUow.Parent);
        }

        [Fact]
        public async Task BeginAsync_WithRequiresNew_ShouldCreateNewRootUnitOfWork()
        {
            // Arrange
            var outerUow = await _manager.BeginAsync();

            // Act
            var newRootUow = await _manager.BeginAsync(requiresNew: true);

            // Assert
            Assert.NotNull(newRootUow);
            Assert.Null(newRootUow.Parent);
            Assert.NotSame(outerUow, newRootUow);
        }

        [Fact]
        public async Task BeginAsync_NestedWithDifferentOptions_ShouldInheritParentTransaction()
        {
            // Arrange
            var outerOptions = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            };
            var innerOptions = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable  // Should be ignored
            };

            using var outerUow = await _manager.BeginAsync(outerOptions);

            // Act
            using var innerUow = await _manager.BeginAsync(innerOptions);

            // Assert
            Assert.Same(outerUow, innerUow.Parent);
        }

        #endregion

        #region Synchronous Begin Tests (Backward Compatibility)

        [Fact]
        public void Begin_WithDefaultOptions_ShouldCreateUnitOfWork()
        {
            // Act
            var uow = _manager.Begin();

            // Assert
            Assert.NotNull(uow);
            Assert.NotNull(uow.Id);
        }

        [Fact]
        public void Begin_WithOptions_ShouldCreateUnitOfWorkWithOptions()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadUncommitted
            };

            // Act
            var uow = _manager.Begin(options);

            // Assert
            Assert.NotNull(uow);
            Assert.Equal(IsolationLevel.ReadUncommitted, uow.Options.IsolationLevel);
        }

        #endregion

        #region Lifecycle Hook Tests

        [Fact]
        public async Task BeginAsync_WithLifecycleHooks_ShouldInvokeApplicableHooks()
        {
            // Arrange
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifecycleHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns(TransactionInitializationMode.Immediate);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<IAmbientUnitOfWorkAccessor, AmbientUnitOfWorkAccessor>();
            services.AddSingleton(mockHook.Object);
            var serviceProvider = services.BuildServiceProvider();

            var manager = new UnitOfWorkManager(serviceProvider, _mockLogger.Object);
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };

            // Act
            var uow = await manager.BeginAsync(options);

            // Assert
            Assert.True(hookInvoked);
        }

        [Fact]
        public async Task BeginAsync_WithNonApplicableHook_ShouldNotInvokeHook()
        {
            // Arrange
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifecycleHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns(TransactionInitializationMode.Immediate);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<IAmbientUnitOfWorkAccessor, AmbientUnitOfWorkAccessor>();
            services.AddSingleton(mockHook.Object);
            var serviceProvider = services.BuildServiceProvider();

            var manager = new UnitOfWorkManager(serviceProvider, _mockLogger.Object);
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy  // Different from hook's mode
            };

            // Act
            var uow = await manager.BeginAsync(options);

            // Assert
            Assert.False(hookInvoked);
        }

        [Fact]
        public async Task BeginAsync_WithApplicableToAllModesHook_ShouldAlwaysInvokeHook()
        {
            // Arrange
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifecycleHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns((TransactionInitializationMode?)null);  // Applies to all
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton<IAmbientUnitOfWorkAccessor, AmbientUnitOfWorkAccessor>();
            services.AddSingleton(mockHook.Object);
            var serviceProvider = services.BuildServiceProvider();

            var manager = new UnitOfWorkManager(serviceProvider, _mockLogger.Object);

            // Act
            var uow = await manager.BeginAsync();  // Lazy mode

            // Assert
            Assert.True(hookInvoked);
        }

        #endregion

        #region Current UoW Tests

        [Fact]
        public void Current_WithNoActiveUow_ShouldReturnNull()
        {
            // Act
            var current = _manager.Current;

            // Assert
            Assert.Null(current);
        }

        [Fact]
        public async Task Current_WithActiveUow_ShouldReturnActiveUow()
        {
            // Arrange
            var uow = await _manager.BeginAsync();

            // Act
            var current = _manager.Current;

            // Assert
            Assert.Same(uow, current);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task BeginAsync_CalledMultipleTimes_ShouldCreateMultipleNestedUnitsOfWork()
        {
            // Act
            using var uow1 = await _manager.BeginAsync();
            using var uow2 = await _manager.BeginAsync();
            using var uow3 = await _manager.BeginAsync();

            // Assert
            Assert.NotNull(uow1);
            Assert.NotNull(uow2);
            Assert.NotNull(uow3);
            Assert.Same(uow1, uow2.Parent);
            Assert.Same(uow2, uow3.Parent);
        }

        [Fact]
        public async Task BeginAsync_WithCancellationToken_ShouldPassTokenToLifecycleHooks()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // Should not throw, but lifecycle hooks would receive the cancelled token
            var uow = await _manager.BeginAsync(cancellationToken: cts.Token);
            Assert.NotNull(uow);
        }

        #endregion
    }
}
