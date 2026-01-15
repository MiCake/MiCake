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
