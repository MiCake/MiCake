using MiCake.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Tests for MiCakeInterceptorFactory to ensure proper dependency injection behavior
    /// </summary>
    public class MiCakeInterceptorFactoryTests
    {
        public MiCakeInterceptorFactoryTests()
        {
            // Reset helper factory state before each test to ensure isolation
            MiCakeInterceptorFactoryHelper.Reset();
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MiCakeInterceptorFactory(null));
        }

        [Fact]
        public void Constructor_WithValidServiceProvider_ShouldSucceed()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Assert
            Assert.NotNull(factory);
            Assert.True(factory.CanCreateInterceptor);
        }

        [Fact]
        public void CanCreateInterceptor_WithValidServiceProvider_ShouldReturnTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act & Assert
            Assert.True(factory.CanCreateInterceptor);
        }

        [Fact]
        public void CreateInterceptor_WithAvailableLifetimeService_ShouldReturnInterceptor()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act
            var interceptor = factory.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor);
        }

        [Fact]
        public void CreateInterceptor_WithoutLifetimeService_ShouldThrowException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateInterceptor());
            Assert.Contains("No service for type 'MiCake.EntityFrameworkCore.IEFSaveChangesLifetime' has been registered", exception.Message);
        }

        [Fact]
        public void CreateInterceptor_CalledMultipleTimes_ShouldReturnNewInstances()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act
            var interceptor1 = factory.CreateInterceptor();
            var interceptor2 = factory.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor1);
            Assert.NotNull(interceptor2);
            Assert.NotSame(interceptor1, interceptor2); // Should be different instances (not cached)
        }

        [Fact]
        public void CreateInterceptor_WhenServiceResolutionFails_ShouldThrowException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.Dispose(); // Dispose to make service resolution fail
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<ObjectDisposedException>(() => factory.CreateInterceptor());
            Assert.Contains("Cannot access a disposed object", exception.Message);
        }

        // Tests for MiCakeInterceptorFactoryHelper (backward compatibility static API)

        [Fact]
        public void Helper_IsConfigured_InitialState_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.False(MiCakeInterceptorFactoryHelper.IsConfigured);
        }

        [Fact]
        public void Helper_Configure_WithValidFactory_ShouldMarkAsConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);

            // Act
            MiCakeInterceptorFactoryHelper.Configure(factory);

            // Assert
            Assert.True(MiCakeInterceptorFactoryHelper.IsConfigured);
        }

        [Fact]
        public void Helper_Configure_WithNullFactory_ShouldNotThrow()
        {
            // Act & Assert (should not throw)
            MiCakeInterceptorFactoryHelper.Configure(null);
            Assert.False(MiCakeInterceptorFactoryHelper.IsConfigured);
        }

        [Fact]
        public void Helper_CreateInterceptor_WhenNotConfigured_ShouldReturnNull()
        {
            // Act
            var interceptor = MiCakeInterceptorFactoryHelper.CreateInterceptor();

            // Assert
            Assert.Null(interceptor);
        }

        [Fact]
        public void Helper_CreateInterceptor_WhenConfiguredWithValidFactory_ShouldReturnInterceptor()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            // Act
            var interceptor = MiCakeInterceptorFactoryHelper.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor);
        }

        [Fact]
        public void Helper_CreateInterceptor_WhenFactoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var services = new ServiceCollection();
            // Don't register IEFSaveChangesLifetime so service resolution fails
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => MiCakeInterceptorFactoryHelper.CreateInterceptor());
            Assert.Contains("No service for type 'MiCake.EntityFrameworkCore.IEFSaveChangesLifetime' has been registered", exception.Message);
        }

        [Fact]
        public void Helper_CreateInterceptor_CalledMultipleTimes_ShouldReturnNewInstances()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            // Act
            var interceptor1 = MiCakeInterceptorFactoryHelper.CreateInterceptor();
            var interceptor2 = MiCakeInterceptorFactoryHelper.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor1);
            Assert.NotNull(interceptor2);
            Assert.NotSame(interceptor1, interceptor2); // Should be different instances (not cached in new design)
        }

        [Fact]
        public void Helper_Reset_AfterConfiguration_ShouldClearConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);
            MiCakeInterceptorFactoryHelper.Configure(factory);
            Assert.True(MiCakeInterceptorFactoryHelper.IsConfigured);

            // Act
            MiCakeInterceptorFactoryHelper.Reset();

            // Assert
            Assert.False(MiCakeInterceptorFactoryHelper.IsConfigured);
            Assert.Null(MiCakeInterceptorFactoryHelper.CreateInterceptor());
        }

        [Fact]
        public void Helper_Configure_CalledTwice_ShouldReplaceExistingConfiguration()
        {
            // Arrange
            var services1 = new ServiceCollection();
            var mockLifetime1 = new MockEFSaveChangesLifetime();
            services1.AddSingleton<IEFSaveChangesLifetime>(mockLifetime1);
            var serviceProvider1 = services1.BuildServiceProvider();
            var factory1 = new MiCakeInterceptorFactory(serviceProvider1);

            var services2 = new ServiceCollection();
            var mockLifetime2 = new MockEFSaveChangesLifetime();
            services2.AddSingleton<IEFSaveChangesLifetime>(mockLifetime2);
            var serviceProvider2 = services2.BuildServiceProvider();
            var factory2 = new MiCakeInterceptorFactory(serviceProvider2);

            MiCakeInterceptorFactoryHelper.Configure(factory1);
            var interceptor1 = MiCakeInterceptorFactoryHelper.CreateInterceptor();

            // Act
            MiCakeInterceptorFactoryHelper.Configure(factory2);
            var interceptor2 = MiCakeInterceptorFactoryHelper.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor1);
            Assert.NotNull(interceptor2);
            Assert.NotSame(interceptor1, interceptor2); // Should be different instances from different factories
            Assert.True(MiCakeInterceptorFactoryHelper.IsConfigured);
        }

        [Fact]
        public async Task Helper_CreateInterceptor_ConcurrentCalls_ShouldWorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLifetime = new MockEFSaveChangesLifetime();
            services.AddSingleton<IEFSaveChangesLifetime>(mockLifetime);
            var serviceProvider = services.BuildServiceProvider();
            var factory = new MiCakeInterceptorFactory(serviceProvider);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            MiCakeEFCoreInterceptor interceptor1 = null;
            MiCakeEFCoreInterceptor interceptor2 = null;
            var task1 = Task.Run(() => interceptor1 = MiCakeInterceptorFactoryHelper.CreateInterceptor());
            var task2 = Task.Run(() => interceptor2 = MiCakeInterceptorFactoryHelper.CreateInterceptor());

            // Act
            await Task.WhenAll(task1, task2);

            // Assert
            Assert.NotNull(interceptor1);
            Assert.NotNull(interceptor2);
            // Both should succeed (no requirement that they must be the same instance)
        }

        /// <summary>
        /// Mock implementation of IEFSaveChangesLifetime for testing purposes
        /// </summary>
        private class MockEFSaveChangesLifetime : IEFSaveChangesLifetime
        {
            public Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}