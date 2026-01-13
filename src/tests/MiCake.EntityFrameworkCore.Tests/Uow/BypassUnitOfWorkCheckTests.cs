using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for BypassUnitOfWorkCheck functionality in EFCoreContextFactory.
    /// Tests verify that repositories can optionally bypass UoW checks for read-only scenarios.
    /// </summary>
    public class BypassUnitOfWorkCheckTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<ILogger<EFCoreContextFactory<TestDbContext>>> _mockLogger;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockWrapperLogger;
        private readonly TestDbContext _dbContext;

        public BypassUnitOfWorkCheckTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockLogger = new Mock<ILogger<EFCoreContextFactory<TestDbContext>>>();
            _mockWrapperLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new TestDbContext(options);

            // Setup service provider to return DbContext and logger
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(TestDbContext)))
                .Returns(_dbContext);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<EFCoreDbContextWrapper>)))
                .Returns(_mockWrapperLogger.Object);
        }

        #region BypassUnitOfWorkCheck = false (Default Behavior) Tests

        [Fact]
        public void GetDbContext_WithoutUoW_WhenBypassDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = false // Default value
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());
            Assert.Contains("No active Unit of Work", exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions.BypassUnitOfWorkCheck), exception.Message);
        }

        [Fact]
        public void GetDbContextWrapper_WithoutUoW_WhenBypassDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = false
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContextWrapper());
            Assert.Contains("No active Unit of Work", exception.Message);
        }

        [Fact]
        public void GetDbContext_WithUoW_WhenBypassDisabled_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = false
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            mockUow.As<IUnitOfWorkInternal>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result);
        }

        #endregion

        #region BypassUnitOfWorkCheck = true Tests

        [Fact]
        public void GetDbContext_WithoutUoW_WhenBypassEnabled_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result);
        }

        [Fact]
        public void GetDbContextWrapper_WithoutUoW_WhenBypassEnabled_ShouldReturnWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetDbContextWrapper();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result.DbContext);
        }

        [Fact]
        public void GetDbContext_WithoutUoW_WhenBypassEnabled_ShouldLogWarning()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            factory.GetDbContext();

            // Assert - Verify warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BypassUnitOfWorkCheck")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetDbContext_WithUoW_WhenBypassEnabled_ShouldStillRegisterWithUoW()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var wrapper = factory.GetDbContextWrapper();

            // Assert - Even with bypass enabled, when UoW is present, it should register
            Assert.NotNull(wrapper);
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        #endregion

        #region Default Value Tests

        [Fact]
        public void BypassUnitOfWorkCheck_DefaultValue_ShouldBeFalse()
        {
            // Arrange & Act
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext));

            // Assert
            Assert.False(options.BypassUnitOfWorkCheck);
        }

        [Fact]
        public void BypassUnitOfWorkCheck_CanBeSetToTrue()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext));

            // Act
            options.BypassUnitOfWorkCheck = true;

            // Assert
            Assert.True(options.BypassUnitOfWorkCheck);
        }

        #endregion

        #region Integration Tests with Real DI

        [Fact]
        public async Task Integration_WithBypassEnabled_ShouldWorkWithoutUoW()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

            // Enable bypass
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();

            // Act - Access DbContext without starting UoW
            var dbContext = factory.GetDbContext();

            // Assert
            Assert.NotNull(dbContext);

            // Verify we can perform read operations
            var count = await dbContext.Set<SampleEntity>().CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Integration_WithBypassEnabled_ShouldStillWorkWithUoW()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

            // Enable bypass
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
            var uowManager = provider.GetRequiredService<IUnitOfWorkManager>();
            var dbContext = provider.GetRequiredService<TestDbContext>();

            // Act - Use DbContext within UoW (normal pattern)
            using (var uow = await uowManager.BeginAsync())
            {
                var wrapper = factory.GetDbContextWrapper();
                dbContext.Set<SampleEntity>().Add(new SampleEntity { Name = "Test" });
                await uow.CommitAsync();
            }

            // Assert
            var count = await dbContext.Set<SampleEntity>().CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Integration_WithBypassDisabled_ShouldThrowWithoutUoW()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

            // Bypass disabled (default)
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = false
            };
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();

            // Act & Assert - Should throw without UoW
            Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());
        }

        #endregion

        #region Error Message Quality Tests

        [Fact]
        public void GetDbContext_WithoutUoW_ErrorMessageShouldContainHelpfulInformation()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                BypassUnitOfWorkCheck = false
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());

            // Assert - Error message should contain helpful information
            Assert.Contains("No active Unit of Work", exception.Message);
            Assert.Contains("TestDbContext", exception.Message);
            Assert.Contains("unitOfWorkManager.Begin()", exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions), exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions.BypassUnitOfWorkCheck), exception.Message);
        }

        #endregion

        #region Helper Classes

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<SampleEntity> SampleEntities { get; set; } = null!;
        }

        public class SampleEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class ObjectAccessor<T> : IObjectAccessor<T>
        {
            public ObjectAccessor(T value) => Value = value;
            public T Value { get; }
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
