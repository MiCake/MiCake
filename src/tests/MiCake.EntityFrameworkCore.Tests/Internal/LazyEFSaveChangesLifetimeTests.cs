using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Tests for LazyEFSaveChangesLifetime to ensure proper service lifecycle management
    /// and correct execution behavior with different scenarios.
    /// </summary>
    public class LazyEFSaveChangesLifetimeTests
    {
        [Fact]
        public void Constructor_WithValidServiceProvider_ShouldSucceed()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var serviceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();

            // Act
            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);

            // Assert
            Assert.NotNull(lifetime);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new LazyEFSaveChangesLifetime(null!));
            Assert.Equal("serviceProvider", exception.ParamName);
        }

        [Fact]
        public async Task AfterSaveChangesAsync_WithEmptyEntityEntries_ShouldReturnImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var serviceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);
            var emptyEntries = new List<EntityEntry>();

            // Act & Assert - Should complete without exception
            await lifetime.AfterSaveChangesAsync(emptyEntries, CancellationToken.None);
        }

        [Fact]
        public async Task BeforeSaveChangesAsync_WithEmptyEntityEntries_ShouldReturnImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var serviceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);
            var emptyEntries = new List<EntityEntry>();

            // Act & Assert - Should complete without exception
            await lifetime.BeforeSaveChangesAsync(emptyEntries, CancellationToken.None);
        }

        [Fact]
        public async Task AfterSaveChangesAsync_WithNoHandlers_ShouldReturnImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var serviceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);
            
            // Use empty collection since we can't easily mock EntityEntry
            var entries = new List<EntityEntry>();

            // Act & Assert - Should complete without exception
            await lifetime.AfterSaveChangesAsync(entries, CancellationToken.None);
        }

        [Fact]
        public async Task BeforeSaveChangesAsync_WithNoHandlers_ShouldReturnImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var serviceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);
            
            // Use empty collection since we can't easily mock EntityEntry
            var entries = new List<EntityEntry>();

            // Act & Assert - Should complete without exception
            await lifetime.BeforeSaveChangesAsync(entries, CancellationToken.None);
        }

        [Fact]
        public async Task BeforeAndAfterSaveChanges_WithTrackedEntity_InvokeHandlersWithPreSaveState()
        {
            // Arrange
            var services = new ServiceCollection();
            var preHandler = new RecordingPreSaveChangesHandler();
            var postHandler = new RecordingPostSaveChangesHandler();
            services.AddSingleton<IRepositoryPreSaveChanges>(preHandler);
            services.AddSingleton<IRepositoryPostSaveChanges>(postHandler);
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new DbContextOptionsBuilder<LifetimeTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new LifetimeTestDbContext(options);
            var entry = context.Add(new LifetimeTestEntity { Name = "tracked" });

            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);

            // Act
            await lifetime.BeforeSaveChangesAsync(new[] { entry }, CancellationToken.None);
            await lifetime.AfterSaveChangesAsync(new[] { entry }, CancellationToken.None);

            // Assert
            Assert.Equal(1, preHandler.CallCount);
            Assert.Equal(1, postHandler.CallCount);
        }

        [Fact]
        public async Task BeforeSaveChanges_HandlerStateOverride_IsAppliedToEntry()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRepositoryPreSaveChanges, ModifiedStatePreSaveChangesHandler>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new DbContextOptionsBuilder<LifetimeTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new LifetimeTestDbContext(options);
            var entry = context.Add(new LifetimeTestEntity { Name = "override" });

            var lifetime = new LazyEFSaveChangesLifetime(serviceProvider);

            // Act
            await lifetime.BeforeSaveChangesAsync(new[] { entry }, CancellationToken.None);

            // Assert - the handler-returned repository state was applied to the live entry.
            Assert.Equal(EntityState.Modified, entry.State);
        }
    }

    /// <summary>
    /// Test implementation of IRepositoryPreSaveChanges for verification
    /// </summary>
    internal class RecordingPreSaveChangesHandler : IRepositoryPreSaveChanges
    {
        public int CallCount { get; private set; }
        public int Order { get; set; }

        public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
            RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(entityState);
        }
    }

    /// <summary>
    /// Test implementation of IRepositoryPostSaveChanges for verification
    /// </summary>
    internal class RecordingPostSaveChangesHandler : IRepositoryPostSaveChanges
    {
        public int CallCount { get; private set; }
        public int Order { get; set; }

        public ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
            RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(entityState);
        }
    }

    /// <summary>
    /// Pre-save handler that always requests the Modified repository state.
    /// </summary>
    internal class ModifiedStatePreSaveChangesHandler : IRepositoryPreSaveChanges
    {
        public int Order { get; set; }

        public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
            RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RepositoryEntityStates.Modified);
        }
    }

    internal class LifetimeTestDbContext : DbContext
    {
        public LifetimeTestDbContext(DbContextOptions<LifetimeTestDbContext> options) : base(options)
        {
        }

        public DbSet<LifetimeTestEntity> Entities => Set<LifetimeTestEntity>();
    }

    internal class LifetimeTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}