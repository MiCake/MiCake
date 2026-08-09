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
    /// Unit tests for AllowDbContextAccessWithoutUoW functionality in EFCoreContextFactory.
    /// Tests verify that repositories can optionally access DbContext without a UoW for
    /// read-only scenarios, and that writes remain guarded regardless of the option.
    /// </summary>
    public class AllowDbContextAccessWithoutUoWTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<ILogger<EFCoreContextFactory<TestDbContext>>> _mockLogger;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockWrapperLogger;
        private readonly TestDbContext _dbContext;

        public AllowDbContextAccessWithoutUoWTests()
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

        #region AllowDbContextAccessWithoutUoW = false (Default Behavior) Tests

        [Fact]
        public void GetDbContext_WithoutUoW_WhenBypassDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = false // Default value
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
            Assert.Contains(nameof(MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW), exception.Message);
        }

        [Fact]
        public void GetDbContextWrapper_WithoutUoW_WhenDisallowed_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
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
        }

        [Fact]
        public void GetDbContext_WithUoW_WhenDisallowed_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
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

        #region AllowDbContextAccessWithoutUoW = true Tests

        [Fact]
        public void GetDbContext_WithoutUoW_WhenAllowed_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
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
        public void GetOrCreateWrapperFor_WithoutUoW_WhenAllowed_ShouldReturnWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
            };
            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            var factory = new EFCoreContextFactory<TestDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result.DbContext);
        }

        [Fact]
        public void GetDbContext_WithoutUoW_WhenAllowed_ShouldLogWarning()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AllowDbContextAccessWithoutUoW")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetDbContext_WithUoW_WhenAllowed_ShouldStillRegisterWithUoW()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
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
            var wrapper = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert - Even with bypass enabled, when UoW is present, it should register
            Assert.NotNull(wrapper);
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        #endregion

        #region Default Value Tests

        [Fact]
        public void AllowDbContextAccessWithoutUoW_DefaultValue_ShouldBeFalse()
        {
            // Arrange & Act
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext));

            // Assert
            Assert.False(options.AllowDbContextAccessWithoutUoW);
        }

        [Fact]
        public void AllowDbContextAccessWithoutUoW_CanBeSetToTrue()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext));

            // Act
            options.AllowDbContextAccessWithoutUoW = true;

            // Assert
            Assert.True(options.AllowDbContextAccessWithoutUoW);
        }

        #endregion

        #region Integration Tests with Real DI

        [Fact]
        public async Task Integration_WithAccessAllowed_ShouldWorkWithoutUoW()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

            // Allow context access without a UoW
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
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
        public async Task Integration_WithAccessAllowed_ShouldStillWorkWithUoW()
        {
            // Arrange - file-backed SQLite because the UoW now requires explicit transactions,
            // which the InMemory provider does not support.
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"micake-access-{Guid.NewGuid():N}.db");
            ServiceProvider? provider = null;
            try
            {
                var services = new ServiceCollection();
                services.AddDbContext<TestDbContext>(opt => opt.UseSqlite($"Data Source={dbPath};Pooling=False"));
                services.AddLogging();

                var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
                var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
                services.AddSingleton(ambientAccessorType!);
                services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
                services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
                services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

                // Allow context access without a UoW
                var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
                {
                    AllowDbContextAccessWithoutUoW = true
                };
                services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

                var provider2 = services.BuildServiceProvider();
                provider = provider2;

                var factory = provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
                var uowManager = provider.GetRequiredService<IUnitOfWorkManager>();
                var dbContext = provider.GetRequiredService<TestDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                // Act - Use DbContext within UoW (normal pattern)
                using (var uow = await uowManager.BeginAsync())
                {
                    var wrapper = factory.GetOrCreateWrapperFor(dbContext);
                    dbContext.Set<SampleEntity>().Add(new SampleEntity { Name = "Test" });
                    await uow.CommitAsync();
                }

                // Assert
                var count = await dbContext.Set<SampleEntity>().CountAsync();
                Assert.Equal(1, count);
            }
            finally
            {
                provider?.Dispose();
                if (System.IO.File.Exists(dbPath))
                {
                    System.IO.File.Delete(dbPath);
                }
            }
        }

        [Fact]
        public void Integration_WithAccessDenied_ShouldThrowWithoutUoW()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));

            // Access denied (default)
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
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
                AllowDbContextAccessWithoutUoW = false
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
            Assert.Contains("unitOfWorkManager.BeginAsync()", exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions), exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW), exception.Message);
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
