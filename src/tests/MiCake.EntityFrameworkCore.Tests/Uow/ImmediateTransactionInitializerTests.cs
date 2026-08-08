using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for ImmediateTransactionInitializer: typed enumeration of every registered
    /// non-generic context factory view (custom factories adapted via
    /// <see cref="EFCoreContextFactoryAdapter"/>), deduplication, and resource anchoring.
    /// </summary>
    public class ImmediateTransactionInitializerTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<ImmediateTransactionInitializer>> _mockLogger;

        public ImmediateTransactionInitializerTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionInitializer>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(initializer);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(_mockServiceProvider.Object, null!));
        }

        #endregion

        #region InitializeTransactionsAsync Tests

        [Fact]
        public async Task InitializeTransactionsAsync_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Arrange
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                initializer.InitializeTransactionsAsync(null!));
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithNoFactories_ShouldLogWarning()
        {
            // Arrange
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(Array.Empty<IEFCoreContextFactory>());

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No DbContext factories registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithRegisteredFactories_ShouldActivateEachRegisteredFactory()
        {
            // Arrange
            var wrapper = CreateMockWrapper();

            var factory1 = new FakeContextFactory { Wrapper = wrapper.Object };
            var factory2 = new FakeContextFactory { Wrapper = wrapper.Object };

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(new IEFCoreContextFactory[] { factory1, factory2 });

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            mockUow.As<IUnitOfWorkInternal>();

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            Assert.Equal(1, factory1.ResolveCount);
            Assert.Equal(1, factory2.ResolveCount);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_ShouldPassTheUnitOfWorkToDiagnostics()
        {
            // Arrange
            var wrapper = CreateMockWrapper();
            var factory = new FakeContextFactory { Wrapper = wrapper.Object };

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(new IEFCoreContextFactory[] { factory });

            var uowId = Guid.NewGuid();
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(uowId);
            mockUow.As<IUnitOfWorkInternal>();

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object, CancellationToken.None);

            // Assert
            Assert.Equal(1, factory.ResolveCount);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithDuplicateFactoriesForSameContextType_ShouldRegisterResourceOnce()
        {
            // Arrange - two internal factory views (e.g. from repeated AddUowCoreServices
            // calls) wrapping the same DbContext type must initialize the resource once.
            var wrapper = CreateMockWrapper();
            var factory1 = new FakeContextFactory { Wrapper = wrapper.Object };
            var factory2 = new FakeContextFactory { Wrapper = wrapper.Object };

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(new IEFCoreContextFactory[] { factory1, factory2 });

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert - every internal view is enumerated, but the resource is registered
            // exactly once because both views resolve the same DbContext type.
            Assert.Equal(1, factory1.ResolveCount);
            Assert.Equal(1, factory2.ResolveCount);
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        [Fact]
        public void AnchorResource_WithUoWNotImplementingInternal_ShouldThrow()
        {
            // Arrange - a live UoW that cannot own resources fails fast with the same
            // diagnostic the framework factory uses, instead of silently skipping registration.
            var options = new DbContextOptionsBuilder<TestInitializerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new TestInitializerDbContext(options);
            var wrapper = new EFCoreDbContextWrapper(context, NullLogger<EFCoreDbContextWrapper>.Instance);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EFCoreContextResourceAnchor.AnchorResource(
                    mockUow.Object,
                    context,
                    wrapper,
                    nameof(TestInitializerDbContext)));
            Assert.Contains(nameof(IUnitOfWorkInternal), exception.Message);

            context.Dispose();
        }

        [Fact]
        public void Adapter_WhenCustomFactoryThrows_PreservesOriginalException()
        {
            // Arrange - a custom factory that fails to resolve its context must surface
            // the original exception, not a reflection wrapper.
            var mockFactory = new Mock<IEFCoreContextFactory<TestInitializerDbContext>>();
            mockFactory.Setup(f => f.GetDbContext())
                .Throws(new InvalidOperationException("custom failure"));

            var adapter = new EFCoreContextFactoryAdapter(
                mockFactory.Object,
                typeof(IEFCoreContextFactory<TestInitializerDbContext>));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => adapter.GetOrCreateWrapperForCurrentUnitOfWork());
            Assert.Equal("custom failure", exception.Message);
        }

        [Fact]
        public void Adapter_WhenCustomFactoryReturnsDifferentContext_AnchorRejects()
        {
            // Arrange - the adapter exposes the resolved context alongside the wrapper so
            // the initializer can reject a wrapper bound to a different context instance.
            var options = new DbContextOptionsBuilder<TestInitializerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var resolvedContext = new TestInitializerDbContext(options);
            var otherContext = new TestInitializerDbContext(options);
            var otherWrapper = new EFCoreDbContextWrapper(otherContext, NullLogger<EFCoreDbContextWrapper>.Instance);

            var mockFactory = new Mock<IEFCoreContextFactory<TestInitializerDbContext>>();
            mockFactory.Setup(f => f.GetDbContext()).Returns(resolvedContext);
            mockFactory.Setup(f => f.GetOrCreateWrapperFor(It.IsAny<DbContext>())).Returns(otherWrapper);

            var adapter = new EFCoreContextFactoryAdapter(
                mockFactory.Object,
                typeof(IEFCoreContextFactory<TestInitializerDbContext>));

            // Act
            var resolution = adapter.GetOrCreateWrapperForCurrentUnitOfWork();
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.As<IUnitOfWorkInternal>();

            // Assert - the anchor rejects the mismatch before any transaction can activate
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EFCoreContextResourceAnchor.AnchorResource(
                    mockUow.Object,
                    resolution.Context,
                    resolution.Wrapper,
                    nameof(TestInitializerDbContext)));
            Assert.Contains("different DbContext instance", exception.Message, StringComparison.OrdinalIgnoreCase);

            otherContext.Dispose();
            resolvedContext.Dispose();
        }

        #endregion

        /// <summary>
        /// Hand-written fake of the internal non-generic factory view (Moq cannot proxy
        /// internal interfaces without DynamicProxyGenAssembly2 friend access).
        /// </summary>
        private sealed class FakeContextFactory : IEFCoreContextFactory
        {
            public EFCoreDbContextWrapper? Wrapper { get; set; }

            public int ResolveCount { get; private set; }

            public EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context)
            {
                ResolveCount++;
                return Wrapper!;
            }

            public EFCoreContextResourceResolution GetOrCreateWrapperForCurrentUnitOfWork()
            {
                ResolveCount++;
                return new EFCoreContextResourceResolution(Wrapper!.DbContext, Wrapper);
            }
        }

        private static Mock<EFCoreDbContextWrapper> CreateMockWrapper()
        {
            var options = new DbContextOptionsBuilder<TestInitializerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new TestInitializerDbContext(options);

            return new Mock<EFCoreDbContextWrapper>(
                context,
                Mock.Of<ILogger<EFCoreDbContextWrapper>>(),
                false);
        }
    }

    public class TestInitializerDbContext : DbContext
    {
        public TestInitializerDbContext(DbContextOptions<TestInitializerDbContext> options) : base(options)
        {
        }
    }
}
