using MiCake.Audit.Conventions;
using MiCake.Audit.Tests.Fakes;
using MiCake.DDD.Domain;
using System;
using Xunit;

namespace MiCake.Audit.Tests
{
    /// <summary>
    /// Tests for AuditTimeConvention to validate generic interface support.
    /// </summary>
    public class AuditTimeConvention_Tests
    {
        private readonly AuditTimeConvention _convention;

        public AuditTimeConvention_Tests()
        {
            _convention = new AuditTimeConvention();
        }

        #region CanApply Tests - DateTime

        [Fact]
        public void CanApply_WithIHasCreatedAtDateTime_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanApply_WithIHasUpdatedAtDateTime_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasModificationTimeModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanApply_WithIHasAuditTimestampsDateTime_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasAuditModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region CanApply Tests - DateTimeOffset

        [Fact]
        public void CanApply_WithIHasCreatedAtDateTimeOffset_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeDateTimeOffsetModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanApply_WithIHasUpdatedAtDateTimeOffset_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasModificationTimeDateTimeOffsetModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanApply_WithIHasAuditTimestampsDateTimeOffset_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(HasAuditDateTimeOffsetModel);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region CanApply Tests - Negative Cases

        [Fact]
        public void CanApply_WithoutAuditInterfaces_ShouldReturnFalse()
        {
            // Arrange
            var entityType = typeof(EntityWithoutAudit);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanApply_WithNullEntityType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _convention.CanApply(null));
        }

        #endregion

        #region Configure Tests

        [Fact]
        public void Configure_WithCreatedAtProperty_ShouldNotThrow()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeModel);
            var propertyName = nameof(IHasCreatedAt.CreatedAt);
            var context = new DDD.Infrastructure.Store.PropertyConventionContext();

            // Act & Assert - Should handle gracefully
            _convention.Configure(entityType, propertyName, context);
        }

        [Fact]
        public void Configure_WithUpdatedAtProperty_ShouldNotThrow()
        {
            // Arrange
            var entityType = typeof(HasModificationTimeModel);
            var propertyName = nameof(IHasUpdatedAt.UpdatedAt);
            var context = new DDD.Infrastructure.Store.PropertyConventionContext();

            // Act & Assert
            _convention.Configure(entityType, propertyName, context);
        }

        [Fact]
        public void Configure_WithOtherProperty_ShouldNotThrow()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeModel);
            var propertyName = "Id";
            var context = new DDD.Infrastructure.Store.PropertyConventionContext();

            // Act & Assert
            _convention.Configure(entityType, propertyName, context);
        }

        [Fact]
        public void Configure_WithNullEntityType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new DDD.Infrastructure.Store.PropertyConventionContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _convention.Configure(null, "CreatedAt", context));
        }

        [Fact]
        public void Configure_WithNullPropertyName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeModel);
            var context = new DDD.Infrastructure.Store.PropertyConventionContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _convention.Configure(entityType, null, context));
        }

        [Fact]
        public void Configure_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var entityType = typeof(HasCreationTimeModel);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _convention.Configure(entityType, "CreatedAt", null));
        }

        #endregion

        #region Priority Tests

        [Fact]
        public void Priority_ShouldBe200()
        {
            // Act
            var priority = _convention.Priority;

            // Assert
            Assert.Equal(200, priority);
        }

        #endregion

        #region Mixed Interface Tests

        [Fact]
        public void CanApply_WithBothDateTimeAndDateTimeOffsetInterfaces_ShouldReturnTrue()
        {
            // Arrange - Entity implementing both IHasCreatedAt<DateTime> and IHasCreatedAt<DateTimeOffset>
            var entityType = typeof(MixedTimestampEntity);

            // Act
            var result = _convention.CanApply(entityType);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Helper Classes

        private class EntityWithoutAudit : Entity
        {
            public string Name { get; set; }
        }

        private class MixedTimestampEntity : Entity, 
            IHasCreatedAt<DateTime>, 
            IHasCreatedAt<DateTimeOffset>
        {
            public DateTime CreatedAtDateTime { get; set; }
            public DateTimeOffset CreatedAtDateTimeOffset { get; set; }

            DateTime IHasCreatedAt<DateTime>.CreatedAt
            {
                get => CreatedAtDateTime;
                set => CreatedAtDateTime = value;
            }

            DateTimeOffset IHasCreatedAt<DateTimeOffset>.CreatedAt
            {
                get => CreatedAtDateTimeOffset;
                set => CreatedAtDateTimeOffset = value;
            }
        }

        #endregion
    }
}
