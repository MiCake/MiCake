using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Tests.Fakes;
using MiCake.DDD.Infrastructure;
using System;
using Xunit;

namespace MiCake.Audit.Tests
{
    /// <summary>
    /// Tests for generic audit interfaces with DateTimeOffset support.
    /// Validates the new TimeProvider-based audit functionality.
    /// </summary>
    public class GenericAuditInterface_Tests
    {
        #region DateTimeOffset Creation Time Tests

        [Fact]
        public void AuditCreationTime_DateTimeOffset_AddedState_ShouldSetCreationTime()
        {
            // Arrange
            var fixedTime = new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero);
            var timeProvider = new FakeTimeProvider(fixedTime);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasCreationTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            // Assert
            Assert.Equal(fixedTime, entity.CreatedAt);
        }

        [Fact]
        public void AuditCreationTime_DateTimeOffset_OtherState_ShouldNotSetCreationTime()
        {
            // Arrange
            var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasCreationTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Unchanged));

            // Assert
            Assert.Equal(default(DateTimeOffset), entity.CreatedAt);
        }

        [Fact]
        public void AuditCreationTime_DateTimeOffset_AlreadySet_ShouldNotOverwrite()
        {
            // Arrange
            var originalTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var newTime = new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero);
            var timeProvider = new FakeTimeProvider(newTime);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasCreationTimeDateTimeOffsetModel { CreatedAt = originalTime };

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            // Assert - Should not overwrite existing time
            Assert.Equal(originalTime, entity.CreatedAt);
        }

        #endregion

        #region DateTimeOffset Modification Time Tests

        [Fact]
        public void AuditModificationTime_DateTimeOffset_ModifiedState_ShouldSetUpdateTime()
        {
            // Arrange
            var fixedTime = new DateTimeOffset(2025, 6, 15, 14, 20, 0, TimeSpan.FromHours(8));
            var timeProvider = new FakeTimeProvider(fixedTime);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasModificationTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));

            // Assert
            Assert.NotNull(entity.UpdatedAt);
            Assert.Equal(fixedTime, entity.UpdatedAt.Value);
        }

        [Fact]
        public void AuditModificationTime_DateTimeOffset_OtherState_ShouldNotSetUpdateTime()
        {
            // Arrange
            var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasModificationTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Unchanged));

            // Assert
            Assert.Null(entity.UpdatedAt);
        }

        [Fact]
        public void AuditModificationTime_DateTimeOffset_AlreadySet_ShouldOverwrite()
        {
            // Arrange
            var originalTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var newTime = new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero);
            var timeProvider = new FakeTimeProvider(newTime);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasModificationTimeDateTimeOffsetModel { UpdatedAt = originalTime };

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));

            // Assert - Should overwrite with new time
            Assert.Equal(newTime, entity.UpdatedAt);
        }

        #endregion

        #region Combined Audit Tests (DateTimeOffset)

        [Fact]
        public void AuditObject_DateTimeOffset_AddedThenModified_ShouldSetBothTimes()
        {
            // Arrange
            var creationTime = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var modificationTime = new DateTimeOffset(2025, 1, 15, 14, 30, 0, TimeSpan.Zero);
            
            var creationProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(creationTime));
            var modificationProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(modificationTime));
            
            var entity = new HasAuditDateTimeOffsetModel();

            // Act
            creationProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            modificationProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));

            // Assert
            Assert.Equal(creationTime, entity.CreatedAt);
            Assert.NotNull(entity.UpdatedAt);
            Assert.Equal(modificationTime, entity.UpdatedAt.Value);
        }

        [Fact]
        public void AuditObject_DateTimeOffset_WithTimezoneInfo_ShouldPreserveOffset()
        {
            // Arrange - Different timezones
            var utcTime = new DateTimeOffset(2025, 1, 21, 10, 0, 0, TimeSpan.Zero);
            var tokyoTime = new DateTimeOffset(2025, 1, 21, 19, 0, 0, TimeSpan.FromHours(9)); // +9 hours
            
            var utcProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(utcTime));
            var tokyoProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(tokyoTime));
            
            var utcEntity = new HasCreationTimeDateTimeOffsetModel();
            var tokyoEntity = new HasCreationTimeDateTimeOffsetModel();

            // Act
            utcProvider.ApplyAudit(new AuditOperationContext(utcEntity, RepositoryEntityStates.Added));
            tokyoProvider.ApplyAudit(new AuditOperationContext(tokyoEntity, RepositoryEntityStates.Added));

            // Assert - Offsets should be preserved
            Assert.Equal(TimeSpan.Zero, utcEntity.CreatedAt.Offset);
            Assert.Equal(TimeSpan.FromHours(9), tokyoEntity.CreatedAt.Offset);
        }

        #endregion

        #region Soft Deletion with DateTimeOffset Tests

        [Fact]
        public void SoftDeletion_DateTimeOffset_DeletedState_ShouldSetDeletionTime()
        {
            // Arrange
            var fixedTime = new DateTimeOffset(2025, 1, 21, 16, 45, 0, TimeSpan.Zero);
            var timeProvider = new FakeTimeProvider(fixedTime);
            var provider = new SoftDeletionAuditProvider(timeProvider);
            var entity = new HasDeletionTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));

            // Assert
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            Assert.Equal(fixedTime, entity.DeletedAt.Value);
        }

        [Fact]
        public void SoftDeletion_DateTimeOffset_OtherState_ShouldNotSetDeletionTime()
        {
            // Arrange
            var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var provider = new SoftDeletionAuditProvider(timeProvider);
            var entity = new HasDeletionTimeDateTimeOffsetModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Unchanged));

            // Assert
            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
        }

        [Fact]
        public void FullAudit_DateTimeOffset_CompleteLifecycle_ShouldTrackAllTimestamps()
        {
            // Arrange
            var creationTime = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var modificationTime = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
            var deletionTime = new DateTimeOffset(2025, 12, 31, 23, 59, 0, TimeSpan.Zero);
            
            var creationAuditProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(creationTime));
            var modificationAuditProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(modificationTime));
            var deletionProvider = new SoftDeletionAuditProvider(new FakeTimeProvider(deletionTime));
            
            var entity = new HasAuditWithSoftDeletionDateTimeOffsetModel();

            // Act - Simulate entity lifecycle
            creationAuditProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            modificationAuditProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));
            deletionProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));

            // Assert
            Assert.Equal(creationTime, entity.CreatedAt);
            Assert.NotNull(entity.UpdatedAt);
            Assert.Equal(modificationTime, entity.UpdatedAt.Value);
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            Assert.Equal(deletionTime, entity.DeletedAt.Value);
        }

        #endregion

        #region TimeProvider Injection Tests

        [Fact]
        public void DefaultTimeAuditProvider_WithNullTimeProvider_ShouldUseSystemTimeProvider()
        {
            // Arrange
            var provider = new DefaultTimeAuditProvider(null);
            var entity = new HasCreationTimeDateTimeOffsetModel();
            var beforeTime = DateTimeOffset.UtcNow;

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            var afterTime = DateTimeOffset.UtcNow;

            // Assert - Should use current time
            Assert.True(entity.CreatedAt >= beforeTime);
            Assert.True(entity.CreatedAt <= afterTime);
        }

        [Fact]
        public void DefaultTimeAuditProvider_DefaultConstructor_ShouldUseSystemTimeProvider()
        {
            // Arrange
            var provider = new DefaultTimeAuditProvider();
            var entity = new HasCreationTimeDateTimeOffsetModel();
            var beforeTime = DateTimeOffset.UtcNow;

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            var afterTime = DateTimeOffset.UtcNow;

            // Assert
            Assert.True(entity.CreatedAt >= beforeTime);
            Assert.True(entity.CreatedAt <= afterTime);
        }

        [Fact]
        public void SoftDeletionAuditProvider_WithNullTimeProvider_ShouldUseSystemTimeProvider()
        {
            // Arrange
            var provider = new SoftDeletionAuditProvider(null);
            var entity = new HasDeletionTimeDateTimeOffsetModel();
            var beforeTime = DateTimeOffset.UtcNow;

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));
            var afterTime = DateTimeOffset.UtcNow;

            // Assert
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            Assert.True(entity.DeletedAt.Value >= beforeTime);
            Assert.True(entity.DeletedAt.Value <= afterTime);
        }

        [Fact]
        public void SoftDeletionAuditProvider_DefaultConstructor_ShouldUseSystemTimeProvider()
        {
            // Arrange
            var provider = new SoftDeletionAuditProvider();
            var entity = new HasDeletionTimeDateTimeOffsetModel();
            var beforeTime = DateTimeOffset.UtcNow;

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Deleted));
            var afterTime = DateTimeOffset.UtcNow;

            // Assert
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
            Assert.True(entity.DeletedAt.Value >= beforeTime);
            Assert.True(entity.DeletedAt.Value <= afterTime);
        }

        #endregion

        #region DateTime vs DateTimeOffset Priority Tests

        [Fact]
        public void AuditProvider_WithBothDateTimeAndDateTimeOffsetInterfaces_ShouldPreferDateTimeOffset()
        {
            // Arrange - Entity implementing both IHasCreatedAt (DateTime) and IHasCreatedAt<DateTimeOffset>
            var fixedTime = new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.FromHours(5));
            var timeProvider = new FakeTimeProvider(fixedTime);
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new MixedTimestampEntity();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            // Assert - DateTimeOffset should be set (due to early return in provider logic)
            Assert.Equal(fixedTime, entity.CreatedAtOffset);
            // DateTime version should remain default since DateTimeOffset is handled first
            Assert.Equal(default(DateTime), ((IHasCreatedAt<DateTime>)entity).CreatedAt);
        }

        // Test entity implementing both generic and legacy interfaces
        private class MixedTimestampEntity : DDD.Domain.Entity, 
            IHasCreatedAt<DateTimeOffset>, 
            IHasCreatedAt<DateTime>
        {
            public DateTimeOffset CreatedAtOffset { get; set; }
            
            DateTime IHasCreatedAt<DateTime>.CreatedAt { get; set; }
            
            DateTimeOffset IHasCreatedAt<DateTimeOffset>.CreatedAt 
            { 
                get => CreatedAtOffset;
                set => CreatedAtOffset = value;
            }
        }

        #endregion

        #region Backward Compatibility Tests

        [Fact]
        public void LegacyInterface_DateTime_ShouldStillWork()
        {
            // Arrange - Using legacy IHasCreatedAt (DateTime)
            var fixedTime = new DateTime(2025, 1, 21, 10, 30, 0, DateTimeKind.Utc);
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(fixedTime));
            var provider = new DefaultTimeAuditProvider(timeProvider);
            var entity = new HasCreationTimeModel(); // Legacy model

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            // Assert
            Assert.Equal(fixedTime, entity.CreatedAt);
        }

        [Fact]
        public void LegacyInterface_IHasAuditTimestamps_ShouldWork()
        {
            // Arrange
            var creationTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var modificationTime = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            
            var creationProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(new DateTimeOffset(creationTime)));
            var modificationProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(new DateTimeOffset(modificationTime)));
            
            var entity = new HasAuditModel(); // Legacy model

            // Act
            creationProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));
            modificationProvider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));

            // Assert
            Assert.Equal(creationTime, entity.CreatedAt);
            Assert.NotNull(entity.UpdatedAt);
            Assert.Equal(modificationTime, entity.UpdatedAt.Value);
        }

        #endregion

        #region Null/Edge Case Tests

        [Fact]
        public void AuditProvider_WithNullContext_ShouldNotThrow()
        {
            // Arrange
            var provider = new DefaultTimeAuditProvider();

            // Act & Assert - Should handle gracefully
            provider.ApplyAudit(null);
        }

        [Fact]
        public void AuditProvider_WithNullEntity_ShouldNotThrow()
        {
            // Arrange
            var provider = new DefaultTimeAuditProvider();
            var context = new AuditOperationContext(null, RepositoryEntityStates.Added);

            // Act & Assert
            provider.ApplyAudit(context);
        }

        [Fact]
        public void SoftDeletionProvider_WithNullContext_ShouldNotThrow()
        {
            // Arrange
            var provider = new SoftDeletionAuditProvider();

            // Act & Assert
            provider.ApplyAudit(null);
        }

        [Fact]
        public void SoftDeletionProvider_WithNullEntity_ShouldNotThrow()
        {
            // Arrange
            var provider = new SoftDeletionAuditProvider();
            var context = new AuditOperationContext(null, RepositoryEntityStates.Deleted);

            // Act & Assert
            provider.ApplyAudit(context);
        }

        #endregion
    }
}
