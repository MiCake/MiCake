using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreContextFactory
    /// Tests DbContext creation and wrapper registration with Unit of Work
    /// </summary>
    public class EFCoreContextFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<ILogger<EFCoreContextFactory<TestDbContext>>> _mockLogger;
            var wrapper = _factory.GetDbContextWrapper();
        private readonly EFCoreContextFactory<TestDbContext> _factory;

        public EFCoreContextFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockLogger = new Mock<ILogger<EFCoreContextFactory<TestDbContext>>>();
            _mockUnitOfWorkManager.Verify(m => m.RegisterResource(It.IsAny<IUnitOfWorkResource>()), Times.Once);

            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(_efCoreOptions);

            _factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Arrange
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(_efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestDbContext>(null, _mockUnitOfWorkManager.Object, _mockLogger.Object, optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWorkManager_ShouldThrowArgumentNullException()
        {
            // Arrange
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(_efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestDbContext>(_mockServiceProvider.Object, null, _mockLogger.Object, optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(_efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestDbContext>(_mockServiceProvider.Object, _mockUnitOfWorkManager.Object, null, optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestDbContext>(_mockServiceProvider.Object, _mockUnitOfWorkManager.Object, _mockLogger.Object, null));
        }

        #endregion

        [Fact]
        public async Task GetDbContextWrapper_WithRealUnitOfWork_LazyMode_ShouldAutoRegister()
        {
            // Arrange: use real UoW manager via DI to return wrapper
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(TestDbContext)));

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
            var manager = provider.GetRequiredService<IUnitOfWorkManager>();
            var context = provider.GetRequiredService<TestDbContext>();

            // Act
            using (var uow = await manager.BeginAsync()) // lazy mode
            {
                var wrapper = factory.GetDbContextWrapper();

                context.Set<SampleEntity>().Add(new SampleEntity { Name = "Lazy" });
                await uow.CommitAsync();
            }

            // Assert: registration happened in lazy mode; commit should persist via wrapper
            var count = await context.Set<SampleEntity>().CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetDbContextWrapper_WithRealUnitOfWork_ImmediateMode_ShouldAutoRegister()
        {
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(TestDbContext)));

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
            var manager = provider.GetRequiredService<IUnitOfWorkManager>();
            var context = provider.GetRequiredService<TestDbContext>();

            // Act - Immediate mode initialization should register resources automatically
            using (var uow = await manager.BeginAsync(new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Immediate }))
            {
                // The immediate initializer should have called factory.GetDbContextWrapper for registered types
                var wrapper = factory.GetDbContextWrapper();
                context.Set<SampleEntity>().Add(new SampleEntity { Name = "Immediate" });
                await uow.CommitAsync();
            }

            // Assert: With immediate mode, registration happened earlier, so commit should persist
            var count = await context.Set<SampleEntity>().CountAsync();
            Assert.Equal(1, count);
        }

        #region GetDbContext Tests

        [Fact]
        public void GetDbContext_WithValidSetup_ShouldReturnDbContext()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Returns(dbContext);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            // Act
            var result = _factory.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TestDbContext>(result);
            Assert.Same(dbContext, result);
        }

        [Fact]
        public void GetDbContext_WithoutActiveUnitOfWork_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _factory.GetDbContext());
            Assert.Contains("No active Unit of Work", exception.Message);
            Assert.Contains("TestDbContext", exception.Message);
        }

        [Fact]
        public void GetDbContext_WhenServiceProviderThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Service not registered");
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Throws(expectedException);

            var mockUow = new Mock<IUnitOfWork>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _factory.GetDbContext());
            Assert.Contains("Failed to resolve TestDbContext", exception.Message);
            Assert.Contains("dependency injection", exception.Message.ToLower());
        }

        #endregion

        #region GetDbContextWrapper Tests

        [Fact]
        public void GetDbContextWrapper_WithValidSetup_ShouldReturnWrapperAndRegisterWithUow()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Returns(dbContext);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();
            var mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<ILogger<EFCoreDbContextWrapper>>())
                .Returns(mockLogger.Object);

            // Act
            var wrapper = _factory.GetDbContextWrapper();

            // Assert
            Assert.NotNull(wrapper);
            Assert.IsType<EFCoreDbContextWrapper>(wrapper);
            Assert.Same(dbContext, wrapper.DbContext);

            // Verify registration with UoW
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        [Fact]
        public void GetDbContextWrapper_WithoutActiveUnitOfWork_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _factory.GetDbContextWrapper());
            Assert.Contains("No active Unit of Work", exception.Message);
        }

        [Fact]
        public void GetDbContextWrapper_WhenUowDoesNotImplementInternalInterface_ShouldLogWarning()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Returns(dbContext);

            var mockUow = new Mock<IUnitOfWork>(); // Not implementing IUnitOfWorkInternal
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<ILogger<EFCoreDbContextWrapper>>())
                .Returns(mockLogger.Object);

            // Act
            var wrapper = _factory.GetDbContextWrapper();

            // Assert
            Assert.NotNull(wrapper);
            // Should log warning about not implementing IUnitOfWorkInternal
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("does not implement IUnitOfWorkInternal")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public void IntegrationScenario_RepositoryAccessWithinUow_ShouldWorkCorrectly()
        {
            // Arrange: Simulate repository accessing DbContext within UoW scope
            var dbContext = CreateTestDbContext();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Returns(dbContext);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();
            var mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<ILogger<EFCoreDbContextWrapper>>())
                .Returns(mockLogger.Object);

            // Act: Repository gets DbContext for operations
            var context = _factory.GetDbContext();
            var wrapper = _factory.GetDbContextWrapper();

            // Assert: Both should return the same DbContext instance
            Assert.Same(dbContext, context);
            Assert.Same(dbContext, wrapper.DbContext);

            // Verify wrapper was registered
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        [Fact]
        public void IntegrationScenario_MultipleFactoryCalls_ShouldReuseDbContextWithinSameUow()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<TestDbContext>())
                .Returns(dbContext);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();
            var mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _mockServiceProvider.Setup(sp => sp.GetRequiredService<ILogger<EFCoreDbContextWrapper>>())
                .Returns(mockLogger.Object);

            // Act: Multiple calls to get DbContext
            var context1 = _factory.GetDbContext();
            var context2 = _factory.GetDbContext();
            var wrapper1 = _factory.GetDbContextWrapper();
            var wrapper2 = _factory.GetDbContextWrapper();

            // Assert: All should return the same DbContext instance
            Assert.Same(dbContext, context1);
            Assert.Same(dbContext, context2);
            Assert.Same(dbContext, wrapper1.DbContext);
            Assert.Same(dbContext, wrapper2.DbContext);

            // Verify wrapper was registered only once (since it's the same resource)
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Exactly(2));
        }

        #endregion

        #region Helper Methods

        private TestDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(options);
        }

        #endregion

        #region Test Classes

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }

        #endregion
    }
}