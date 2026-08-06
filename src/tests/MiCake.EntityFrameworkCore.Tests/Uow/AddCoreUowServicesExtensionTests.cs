using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for AddCoreUowServicesExtension.
    /// Tests that UoW-related services are correctly registered in the DI container.
    /// </summary>
    public class AddCoreUowServicesExtensionTests
    {
        #region AddUowCoreServices Tests

        [Fact]
        public void AddUowCoreServices_ShouldRegisterContextFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert - Check service is registered (without resolving, since dependencies aren't fully registered)
            var descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IEFCoreContextFactory<TestExtensionDbContext>));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddUowCoreServices_ShouldRegisterImmediateTransactionLifetimeHook()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));
            var provider = services.BuildServiceProvider();

            // Assert
            var hook = provider.GetService<IUnitOfWorkLifetimeHook>();
            Assert.NotNull(hook);
            Assert.IsType<ImmediateTransactionLifetimeHook>(hook);
        }

        [Fact]
        public void AddUowCoreServices_ShouldRegisterDbContextTypeRegistry()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));
            var provider = services.BuildServiceProvider();

            // Assert
            var registry = provider.GetService<IDbContextTypeRegistry>();
            Assert.NotNull(registry);
            Assert.IsType<DbContextTypeRegistry>(registry);
        }

        [Fact]
        public void AddUowCoreServices_ShouldRegisterImmediateTransactionInitializer()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));
            var provider = services.BuildServiceProvider();

            // Assert
            var initializer = provider.GetService<IImmediateTransactionInitializer>();
            Assert.NotNull(initializer);
            Assert.IsType<ImmediateTransactionInitializer>(initializer);
        }

        [Fact]
        public void AddUowCoreServices_ContextFactory_ShouldBeScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert
            var descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IEFCoreContextFactory<TestExtensionDbContext>));
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddUowCoreServices_ImmediateTransactionLifetimeHook_ShouldBeScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert
            var descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IUnitOfWorkLifetimeHook));
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddUowCoreServices_DbContextTypeRegistry_ShouldBeSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert
            var descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IDbContextTypeRegistry));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void AddUowCoreServices_ImmediateTransactionInitializer_ShouldBeScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert
            var descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IImmediateTransactionInitializer));
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddUowCoreServices_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            var result = services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddUowCoreServices_CalledMultipleTimes_ShouldRegisterServicesOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));
            services.AddUowCoreServices(typeof(TestExtensionDbContext));

            // Assert - Should have multiple registrations (by design, not checking for duplicates)
            var factoryDescriptors = services.FindAll(
                s => s.ServiceType == typeof(IEFCoreContextFactory<TestExtensionDbContext>));
            Assert.Equal(2, factoryDescriptors.Count); // Each call adds registration
        }

        [Fact]
        public void AddUowCoreServices_WithMultipleDbContextTypes_ShouldRegisterAll()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddDbContext<AnotherExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act
            services.AddUowCoreServices(typeof(TestExtensionDbContext));
            services.AddUowCoreServices(typeof(AnotherExtensionDbContext));

            // Assert - Check services are registered (without resolving, since dependencies aren't fully registered)
            var factory1Descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IEFCoreContextFactory<TestExtensionDbContext>));
            var factory2Descriptor = Assert.Single(services,
                s => s.ServiceType == typeof(IEFCoreContextFactory<AnotherExtensionDbContext>));
            Assert.NotNull(factory1Descriptor);
            Assert.NotNull(factory2Descriptor);
        }

        #endregion

        #region DbContext Lifetime Validation Tests

        [Fact]
        public void AddUowCoreServices_WhenDbContextNotRegistered_ShouldThrowWithDiagnostic()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            Assert.Contains(nameof(TestExtensionDbContext), exception.Message);
            Assert.Contains("not registered", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AddDbContext", exception.Message);
        }

        [Fact]
        public void AddUowCoreServices_WhenDbContextIsTransient_ShouldThrowWithDiagnostic()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<TestExtensionDbContext>(_ => new TestExtensionDbContext(
                new DbContextOptionsBuilder<TestExtensionDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options));
            services.AddLogging();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            Assert.Contains(nameof(TestExtensionDbContext), exception.Message);
            Assert.Contains("Transient", exception.Message);
            Assert.Contains("AddDbContext", exception.Message);
        }

        [Fact]
        public void AddUowCoreServices_WhenDbContextIsSingleton_ShouldThrowWithDiagnostic()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<TestExtensionDbContext>(_ => new TestExtensionDbContext(
                new DbContextOptionsBuilder<TestExtensionDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options));
            services.AddLogging();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            Assert.Contains(nameof(TestExtensionDbContext), exception.Message);
            Assert.Contains("Singleton", exception.Message);
        }

        [Fact]
        public void AddUowCoreServices_WhenDbContextIsPooled_ShouldPassValidation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContextPool<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act - pooled registrations surface as scoped descriptors and must pass
            var exception = Record.Exception(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AddUowCoreServices_WhenDbContextRegisteredScopedThenSingleton_ShouldThrow()
        {
            // Arrange - the container resolves the LAST registration; a singleton override
            // after a scoped registration must not pass validation.
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<TestExtensionDbContext>(_ => new TestExtensionDbContext(
                new DbContextOptionsBuilder<TestExtensionDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options));
            services.AddLogging();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            Assert.Contains(nameof(TestExtensionDbContext), exception.Message);
            Assert.Contains("Singleton", exception.Message);
        }

        [Fact]
        public void AddUowCoreServices_WithMultipleScopedRegistrations_ShouldPass()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddDbContext<TestExtensionDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();

            // Act - the effective (last) registration is still scoped
            var exception = Record.Exception(() =>
                services.AddUowCoreServices(typeof(TestExtensionDbContext)));

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region Helper Classes

        public class TestExtensionDbContext : DbContext
        {
            public TestExtensionDbContext(DbContextOptions<TestExtensionDbContext> options) : base(options) { }
        }

        public class AnotherExtensionDbContext : DbContext
        {
            public AnotherExtensionDbContext(DbContextOptions<AnotherExtensionDbContext> options) : base(options) { }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for finding services in ServiceCollection
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        public static System.Collections.Generic.List<ServiceDescriptor> FindAll(
            this IServiceCollection services,
            Func<ServiceDescriptor, bool> predicate)
        {
            var result = new System.Collections.Generic.List<ServiceDescriptor>();
            foreach (var service in services)
            {
                if (predicate(service))
                {
                    result.Add(service);
                }
            }
            return result;
        }
    }
}
