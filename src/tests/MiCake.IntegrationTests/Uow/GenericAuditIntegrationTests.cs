using MiCake.EntityFrameworkCore;
using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MiCake.Core;
using MiCake.Core.Modularity;
using System;
using MiCake.IntegrationTests.Fixtures;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Integration tests for generic audit interfaces with DateTimeOffset support.
    /// </summary>
    [Collection("MiCakeIntegrationTests")]
    public class GenericAuditIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TestDbContext _dbContext;
        private readonly IMiCakeApplication _miCakeApp;
        private readonly MiCakeAppFixture _fixture;

        public GenericAuditIntegrationTests(MiCakeAppFixture fixture)
        {
            _fixture = fixture;
            _serviceProvider = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<TestDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.UseMiCakeInterceptors(sp);
                });

                // Register custom TimeProvider for testing
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero)));

                var builder = services.AddMiCake<TestMiCakeModule>();
                builder.UseEFCore<TestDbContext>();
                builder.UseAudit(opts => opts.UseSoftDeletion = true);
                builder.Build();
            });

            _miCakeApp = _serviceProvider.GetRequiredService<IMiCakeApplication>();
            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        }

        #region DateTimeOffset Creation Time Tests

        [Fact]
        public async Task SaveChanges_WithDateTimeOffset_ShouldSetCreationTime()
        {
            // Arrange
            var entity = new AuditEntityWithCreationTimeDateTimeOffset { Name = "Test Entity" };

            // Act
            _dbContext.AuditDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotEqual(default(DateTimeOffset), entity.CreatedAt);
            // Should use the fixed time from our custom TimeProvider (2025-01-21 10:30:00 UTC)
            Assert.Equal(new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero), entity.CreatedAt);
        }

        [Fact]
        public async Task SaveChanges_WithDateTimeOffset_ShouldNotOverwriteExistingCreationTime()
        {
            // Arrange
            var existingTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var entity = new AuditEntityWithCreationTimeDateTimeOffset
            {
                Name = "Test",
                CreatedAt = existingTime
            };

            // Act
            _dbContext.AuditDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.Equal(existingTime, entity.CreatedAt);
        }

        #endregion

        #region DateTimeOffset Modification Time Tests

        [Fact]
        public async Task SaveChanges_WithDateTimeOffset_ShouldSetModificationTime()
        {
            // Arrange
            var entity = new AuditEntityWithFullDateTimeOffset { Name = "Initial" };
            _dbContext.FullAuditDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Act - Modify entity
            entity.Name = "Modified";
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(entity.UpdatedAt);
            // Should use the fixed time from our custom TimeProvider
            Assert.Equal(new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero), entity.UpdatedAt.Value);
        }

        [Fact]
        public async Task SaveChanges_WithDateTimeOffset_ShouldUpdateModificationTime()
        {
            // Arrange
            var entity = new AuditEntityWithFullDateTimeOffset { Name = "Test" };
            _dbContext.FullAuditDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            var firstUpdateTime = entity.UpdatedAt;
            await Task.Delay(10); // Small delay to ensure time difference

            // Act - Second modification
            entity.Name = "Modified Again";
            await _dbContext.SaveChangesAsync();

            // Assert
            if (firstUpdateTime.HasValue)
            {
                Assert.NotNull(entity.UpdatedAt);
                Assert.True(entity.UpdatedAt >= firstUpdateTime);
            }
        }

        #endregion

        #region DateTimeOffset Soft Deletion Tests

        [Fact]
        public async Task SaveChanges_WithDateTimeOffsetSoftDeletion_ShouldSetDeletionTime()
        {
            // Arrange
            var entity = new SoftDeletableEntityDateTimeOffset { Name = "To Delete" };
            _dbContext.SoftDeleteDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Act
            _dbContext.SoftDeleteDateTimeOffsetEntities.Remove(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            // Should use the fixed time from our custom TimeProvider
            Assert.Equal(new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero), entity.DeletedAt.Value);
        }

        [Fact]
        public async Task SaveChanges_WithDateTimeOffsetSoftDeletion_ShouldKeepEntityInDatabase()
        {
            // Arrange
            var entity = new SoftDeletableEntityDateTimeOffset { Name = "Soft Delete Test" };
            _dbContext.SoftDeleteDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();
            var entityId = entity.Id;

            // Act
            _dbContext.SoftDeleteDateTimeOffsetEntities.Remove(entity);
            await _dbContext.SaveChangesAsync();

            // Assert - Entity should still exist in database (soft deleted)
            var deletedEntity = await _dbContext.SoftDeleteDateTimeOffsetEntities
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == entityId);

            Assert.NotNull(deletedEntity);
            Assert.True(deletedEntity.IsDeleted);
            Assert.NotNull(deletedEntity.DeletedAt);
        }

        #endregion

        #region Complete Lifecycle Tests

        [Fact]
        public async Task CompleteLifecycle_WithDateTimeOffset_ShouldTrackAllTimestamps()
        {
            // Arrange - Create
            var entity = new SoftDeletableEntityDateTimeOffset { Name = "Lifecycle Test" };
            _dbContext.SoftDeleteDateTimeOffsetEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            var createdAt = entity.CreatedAt;
            Assert.NotEqual(default(DateTimeOffset), createdAt);

            // Act - Modify
            await Task.Delay(10);
            entity.Name = "Modified";
            await _dbContext.SaveChangesAsync();

            var updatedAt = entity.UpdatedAt;
            Assert.NotNull(updatedAt);
            Assert.True(updatedAt >= createdAt);

            // Act - Delete
            await Task.Delay(10);
            _dbContext.SoftDeleteDateTimeOffsetEntities.Remove(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            Assert.True(entity.DeletedAt >= updatedAt);
            Assert.Equal(createdAt, entity.CreatedAt); // Creation time should not change
        }

        #endregion

        #region Custom TimeProvider Tests

        [Fact]
        public async Task SaveChanges_WithCustomTimeProvider_ShouldUseProvidedTime()
        {
            // Arrange - Create a new scope with different TimeProvider
            var fixedTime = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(5));
            var customScope = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<TestDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.UseMiCakeInterceptors(sp);
                });

                // Register custom TimeProvider with fixed time
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(fixedTime));

                var builder = services.AddMiCake<TestMiCakeModule>();
                builder.UseEFCore<TestDbContext>();
                builder.UseAudit(opts => opts.UseSoftDeletion = false);
                builder.Build();
            });

            try
            {
                var scopedDb = customScope.GetRequiredService<TestDbContext>();
                var entity = new AuditEntityWithCreationTimeDateTimeOffset { Name = "Custom Time Test" };

                // Act
                scopedDb.AuditDateTimeOffsetEntities.Add(entity);
                await scopedDb.SaveChangesAsync();

                // Assert
                Assert.Equal(fixedTime, entity.CreatedAt);
            }
            finally
            {
                _fixture.ReleaseServiceProvider(customScope);
            }
        }

        #endregion

        #region Backward Compatibility Tests

        [Fact]
        public async Task SaveChanges_WithLegacyDateTimeInterface_ShouldStillWork()
        {
            // Arrange
            var entity = new AuditEntityWithLegacyDateTime { Name = "Legacy Test" };

            // Act
            _dbContext.LegacyAuditEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotEqual(default(DateTime), entity.CreatedAt);
            // Should use the fixed time from our custom TimeProvider (converted to DateTime)
            Assert.Equal(new DateTime(2025, 1, 21, 10, 30, 0, DateTimeKind.Utc), entity.CreatedAt);
        }

        #endregion

        public void Dispose()
        {
            _fixture?.ReleaseServiceProvider(_serviceProvider);
        }

        #region Test Infrastructure

        [RelyOn(typeof(EntityFrameworkCore.Modules.MiCakeEFCoreModule), typeof(Modules.MiCakeEssentialModule))]
        private class TestMiCakeModule : MiCakeModule
        {
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<AuditEntityWithCreationTimeDateTimeOffset> AuditDateTimeOffsetEntities { get; set; }
            public DbSet<AuditEntityWithFullDateTimeOffset> FullAuditDateTimeOffsetEntities { get; set; }
            public DbSet<SoftDeletableEntityDateTimeOffset> SoftDeleteDateTimeOffsetEntities { get; set; }
            public DbSet<AuditEntityWithLegacyDateTime> LegacyAuditEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.UseMiCakeConventions();
            }
        }

        private class AuditEntityWithCreationTimeDateTimeOffset : DDD.Domain.AggregateRoot<Guid>, IHasCreatedAt<DateTimeOffset>
        {
            public string Name { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }

        private class AuditEntityWithFullDateTimeOffset : DDD.Domain.AggregateRoot<Guid>, IHasAuditTimestamps<DateTimeOffset>
        {
            public string Name { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        private class SoftDeletableEntityDateTimeOffset : DDD.Domain.AggregateRoot<Guid>, IAuditableWithSoftDeletion<DateTimeOffset>
        {
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
            public DateTimeOffset? DeletedAt { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        private class AuditEntityWithLegacyDateTime : DDD.Domain.AggregateRoot<Guid>, IHasCreatedAt
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _fixedTime;

            public FakeTimeProvider(DateTimeOffset fixedTime)
            {
                _fixedTime = fixedTime;
            }

            public override DateTimeOffset GetUtcNow() => _fixedTime;
        }

        #endregion
    }
}
