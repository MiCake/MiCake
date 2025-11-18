using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for ImmediateTransactionInitializer
    /// Tests immediate transaction initialization for registered DbContext types
    /// </summary>
    public class ImmediateTransactionInitializerTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IDbContextTypeRegistry> _mockTypeRegistry;
        private readonly Mock<ILogger<ImmediateTransactionInitializer>> _mockLogger;
        private readonly ImmediateTransactionInitializer _initializer;

        public ImmediateTransactionInitializerTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockTypeRegistry = new Mock<IDbContextTypeRegistry>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionInitializer>>();
            
            _initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(null, _mockTypeRegistry.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullTypeRegistry_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(_mockServiceProvider.Object, null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(_mockServiceProvider.Object, _mockTypeRegistry.Object, null));
        }

        #endregion

        #region InitializeTransactionsAsync Tests

        [Fact]
        public async Task InitializeTransactionsAsync_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _initializer.InitializeTransactionsAsync(null));
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithNoRegisteredTypes_ShouldCompleteWithoutError()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes()).Returns(new List<Type>());

            // Act
            await _initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert - No exception thrown
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithRegisteredTypes_ShouldInitializeAll()
        {
            // Arrange
            var testDbContextType = typeof(TestDbContext);
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new List<Type> { testDbContextType });

            var mockFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            var mockWrapper = new Mock<EFCoreDbContextWrapper>();
            mockFactory.Setup(f => f.GetDbContextWrapper()).Returns(mockWrapper.Object);

            var factoryType = typeof(IEFCoreContextFactory<TestDbContext>);
            _mockServiceProvider.Setup(sp => sp.GetService(factoryType))
                .Returns(mockFactory.Object);

            // Act
            await _initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            mockFactory.Verify(f => f.GetDbContextWrapper(), Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithUnregisteredFactory_ShouldLogWarning()
        {
            // Arrange
            var testDbContextType = typeof(TestDbContext);
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new List<Type> { testDbContextType });

            var factoryType = typeof(IEFCoreContextFactory<TestDbContext>);
            _mockServiceProvider.Setup(sp => sp.GetService(factoryType))
                .Returns((object)null);

            // Act
            await _initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert - Should log warning and continue (no exception)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No factory registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithFactoryException_ShouldPropagateException()
        {
            // Arrange
            var testDbContextType = typeof(TestDbContext);
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new List<Type> { testDbContextType });

            var mockFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            mockFactory.Setup(f => f.GetDbContextWrapper())
                .Throws(new InvalidOperationException("Factory initialization failed"));

            var factoryType = typeof(IEFCoreContextFactory<TestDbContext>);
            _mockServiceProvider.Setup(sp => sp.GetService(factoryType))
                .Returns(mockFactory.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _initializer.InitializeTransactionsAsync(mockUow.Object));
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithMultipleTypes_ShouldInitializeAll()
        {
            // Arrange
            var type1 = typeof(TestDbContext);
            var type2 = typeof(AnotherTestDbContext);
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new List<Type> { type1, type2 });

            var mockFactory1 = new Mock<IEFCoreContextFactory<TestDbContext>>();
            var mockWrapper1 = new Mock<EFCoreDbContextWrapper>();
            mockFactory1.Setup(f => f.GetDbContextWrapper()).Returns(mockWrapper1.Object);

            var mockFactory2 = new Mock<IEFCoreContextFactory<AnotherTestDbContext>>();
            var mockWrapper2 = new Mock<EFCoreDbContextWrapper>();
            mockFactory2.Setup(f => f.GetDbContextWrapper()).Returns(mockWrapper2.Object);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEFCoreContextFactory<TestDbContext>)))
                .Returns(mockFactory1.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEFCoreContextFactory<AnotherTestDbContext>)))
                .Returns(mockFactory2.Object);

            // Act
            await _initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            mockFactory1.Verify(f => f.GetDbContextWrapper(), Times.Once);
            mockFactory2.Verify(f => f.GetDbContextWrapper(), Times.Once);
        }

        #endregion

        #region Test DbContext Classes

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }

        private class AnotherTestDbContext : DbContext
        {
            public AnotherTestDbContext(DbContextOptions<AnotherTestDbContext> options) : base(options) { }
        }

        #endregion
    }

    /// <summary>
    /// Unit tests for DbContextTypeRegistry
    /// Tests registration and retrieval of DbContext types
    /// </summary>
    public class DbContextTypeRegistryTests
    {
        #region RegisterDbContextType Tests

        [Fact]
        public void RegisterDbContextType_WithValidType_ShouldSucceed()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            var dbContextType = typeof(TestDbContext);

            // Act
            registry.RegisterDbContextType(dbContextType);

            // Assert
            var registered = registry.GetRegisteredTypes();
            Assert.Single(registered);
            Assert.Contains(dbContextType, registered);
        }

        [Fact]
        public void RegisterDbContextType_WithNullType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                registry.RegisterDbContextType(null));
        }

        [Fact]
        public void RegisterDbContextType_WithNonDbContextType_ShouldThrowArgumentException()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            var invalidType = typeof(string);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                registry.RegisterDbContextType(invalidType));
            Assert.Contains("must inherit from DbContext", exception.Message);
        }

        [Fact]
        public void RegisterDbContextType_SameTypeTwice_ShouldOnlyRegisterOnce()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            var dbContextType = typeof(TestDbContext);

            // Act
            registry.RegisterDbContextType(dbContextType);
            registry.RegisterDbContextType(dbContextType);

            // Assert
            var registered = registry.GetRegisteredTypes();
            Assert.Single(registered);
        }

        [Fact]
        public void RegisterDbContextType_MultipleDifferentTypes_ShouldRegisterAll()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            var type1 = typeof(TestDbContext);
            var type2 = typeof(AnotherTestDbContext);

            // Act
            registry.RegisterDbContextType(type1);
            registry.RegisterDbContextType(type2);

            // Assert
            var registered = registry.GetRegisteredTypes();
            Assert.Equal(2, registered.Count);
            Assert.Contains(type1, registered);
            Assert.Contains(type2, registered);
        }

        #endregion

        #region GetRegisteredTypes Tests

        [Fact]
        public void GetRegisteredTypes_WithNoRegistrations_ShouldReturnEmptyList()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            var registered = registry.GetRegisteredTypes();

            // Assert
            Assert.Empty(registered);
        }

        [Fact]
        public void GetRegisteredTypes_WithRegistrations_ShouldReturnReadOnlyList()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            registry.RegisterDbContextType(typeof(TestDbContext));

            // Act
            var registered = registry.GetRegisteredTypes();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Type>>(registered);
        }

        #endregion

        #region Test DbContext Classes

        private class TestDbContext : DbContext
        {
            public TestDbContext() : base(new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options)
            { }
        }

        private class AnotherTestDbContext : DbContext
        {
            public AnotherTestDbContext() : base(new DbContextOptionsBuilder<AnotherTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options)
            { }
        }

        #endregion
    }
}
