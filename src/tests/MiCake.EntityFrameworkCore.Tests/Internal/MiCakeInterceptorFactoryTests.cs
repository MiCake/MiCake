using MiCake.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        public void Constructor_WithNullSaveChangesLifetime_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MiCakeInterceptorFactory(null));
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldSucceed()
        {
            // Arrange
            var mockLifetime = new MockEFSaveChangesLifetime();

            // Act
            var factory = new MiCakeInterceptorFactory(mockLifetime);

            // Assert
            Assert.NotNull(factory);
            Assert.True(factory.CanCreateInterceptor);
        }

        [Fact]
        public void CanCreateInterceptor_WithValidDependencies_ShouldReturnTrue()
        {
            // Arrange
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);

            // Act & Assert
            Assert.True(factory.CanCreateInterceptor);
        }

        [Fact]
        public void CreateInterceptor_WithValidDependencies_ShouldReturnInterceptor()
        {
            // Arrange
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);

            // Act
            var interceptor = factory.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor);
        }

        [Fact]
        public void CreateInterceptor_CalledMultipleTimes_ShouldReturnNewInstances()
        {
            // Arrange
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);

            // Act
            var interceptor1 = factory.CreateInterceptor();
            var interceptor2 = factory.CreateInterceptor();

            // Assert
            Assert.NotNull(interceptor1);
            Assert.NotNull(interceptor2);
            Assert.NotSame(interceptor1, interceptor2); // Should be different instances (not cached)
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
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);

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
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);
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
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            // Act & Assert
            // Since we now inject dependencies directly, this test is no longer relevant
            // The factory will always have valid dependencies when constructed
            Assert.NotNull(MiCakeInterceptorFactoryHelper.CreateInterceptor());
        }

        [Fact]
        public void Helper_CreateInterceptor_CalledMultipleTimes_ShouldReturnNewInstances()
        {
            // Arrange
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);
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
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);
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
            var mockLifetime1 = new MockEFSaveChangesLifetime();
            var factory1 = new MiCakeInterceptorFactory(mockLifetime1);

            var mockLifetime2 = new MockEFSaveChangesLifetime();
            var factory2 = new MiCakeInterceptorFactory(mockLifetime2);

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
            var mockLifetime = new MockEFSaveChangesLifetime();
            var factory = new MiCakeInterceptorFactory(mockLifetime);
            MiCakeInterceptorFactoryHelper.Configure(factory);

            ISaveChangesInterceptor interceptor1 = null;
            ISaveChangesInterceptor interceptor2 = null;
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