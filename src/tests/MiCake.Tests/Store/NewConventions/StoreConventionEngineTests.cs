using MiCake.Audit;
using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using System;
using Xunit;

namespace MiCake.Tests.Store.NewConventions
{
    public class StoreConventionEngineTests
    {
        private readonly StoreConventionEngine _engine;

        public StoreConventionEngineTests()
        {
            _engine = new StoreConventionEngine();
        }

        [Fact]
        public void AddConvention_WithValidConvention_ShouldAddSuccessfully()
        {
            // Arrange
            var convention = new TestEntityConvention();

            // Act
            _engine.AddConvention(convention);

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void AddConvention_WithMultipleConventions_ShouldAddAll()
        {
            // Arrange
            var convention1 = new TestEntityConvention();
            var convention2 = new TestPropertyConvention();

            // Act
            _engine.AddConvention(convention1);
            _engine.AddConvention(convention2);

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void ApplyEntityConventions_WithApplicableEntity_ShouldApplyConventions()
        {
            // Arrange
            var convention = new TestEntityConvention();
            _engine.AddConvention(convention);
            var entityType = typeof(TestApplicableEntity);

            // Act
            var context = _engine.ApplyEntityConventions(entityType);

            // Assert
            Assert.NotNull(context);
            Assert.True(context.EnableSoftDeletion); // TestEntityConvention sets this
        }

        [Fact]
        public void ApplyEntityConventions_WithNonApplicableEntity_ShouldReturnDefaultContext()
        {
            // Arrange
            var convention = new TestEntityConvention();
            _engine.AddConvention(convention);
            var entityType = typeof(TestNonApplicableEntity);

            // Act
            var context = _engine.ApplyEntityConventions(entityType);

            // Assert
            Assert.NotNull(context);
            Assert.False(context.EnableSoftDeletion); // Default value is false
            Assert.True(context.EnableDirectDeletion); // Default value is true
        }


        [Fact]
        public void ApplyPropertyConventions_WithApplicableProperty_ShouldApplyConventions()
        {
            // Arrange
            var convention = new TestPropertyConvention();
            _engine.AddConvention(convention);
            var entityType = typeof(TestApplicableEntity);
            var propertyInfo = entityType.GetProperty("TestProperty");

            // Act
            var context = _engine.ApplyPropertyConventions(entityType, propertyInfo);

            // Assert
            Assert.NotNull(context);
            Assert.True(context.IsIgnored); // TestPropertyConvention sets this
        }

        [Fact]
        public void ApplyPropertyConventions_WithNonApplicableProperty_ShouldReturnDefaultContext()
        {
            // Arrange
            var convention = new TestPropertyConvention();
            _engine.AddConvention(convention);
            var entityType = typeof(TestNonApplicableEntity);
            var propertyInfo = entityType.GetProperty("NonTestProperty");

            // Act
            var context = _engine.ApplyPropertyConventions(entityType, propertyInfo);

            // Assert
            Assert.NotNull(context);
            Assert.False(context.IsIgnored); // Default value
        }

        [Fact]
        public void ApplyPropertyConventions_WithNullEntityType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var propertyInfo = typeof(TestApplicableEntity).GetProperty("TestProperty");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _engine.ApplyPropertyConventions(null, propertyInfo));
        }

        [Fact]
        public void ApplyPropertyConventions_WithNullPropertyInfo_ShouldThrowArgumentNullException()
        {
            // Arrange
            var entityType = typeof(TestApplicableEntity);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _engine.ApplyPropertyConventions(entityType, null));
        }

        [Fact]
        public void ConventionPriority_ShouldBeRespected()
        {
            // Arrange
            var lowPriorityConvention = new LowPriorityEntityConvention();
            var highPriorityConvention = new HighPriorityEntityConvention();

            _engine.AddConvention(lowPriorityConvention);
            _engine.AddConvention(highPriorityConvention);

            var entityType = typeof(TestApplicableEntity);

            // Act
            var context = _engine.ApplyEntityConventions(entityType);

            // Assert
            Assert.NotNull(context);
            // High priority convention should override low priority one
            Assert.False(context.EnableDirectDeletion); // Set by high priority convention
        }

        [Fact]
        public void ApplyEntityConventions_WithBuiltInConventions_ShouldWork()
        {
            // Arrange
            _engine.AddConvention(new SoftDeletionConvention());
            _engine.AddConvention(new AuditTimeConvention());
            var entityType = typeof(TestSoftDeletableEntity);

            // Act
            var context = _engine.ApplyEntityConventions(entityType);

            // Assert
            Assert.NotNull(context);
            Assert.True(context.EnableSoftDeletion);
            Assert.False(context.EnableDirectDeletion);
            Assert.NotNull(context.QueryFilter);
        }

        [Fact]
        public void ApplyPropertyConventions_WithBuiltInConventions_ShouldWork()
        {
            // Arrange
            _engine.AddConvention(new AuditTimeConvention());
            var entityType = typeof(TestAuditableEntity);
            var propertyInfo = entityType.GetProperty(nameof(TestAuditableEntity.CreatedAt));

            // Act
            var context = _engine.ApplyPropertyConventions(entityType, propertyInfo);

            // Assert
            Assert.NotNull(context);
            // AuditTimeConvention doesn't modify context, just validates interface implementation
        }
    }

    // Test conventions for engine testing
    public class TestEntityConvention : IEntityConvention
    {
        public int Priority => 100;

        public bool CanApply(Type entityType)
        {
            return entityType.Name == "TestApplicableEntity";
        }

        public void Configure(Type entityType, EntityConventionContext context)
        {
            context.EnableSoftDeletion = true;
        }
    }

    public class TestPropertyConvention : IPropertyConvention
    {
        public int Priority => 100;

        public bool CanApply(Type entityType)
        {
            return entityType.Name.Contains("Applicable");
        }

        public void Configure(Type entityType, string propertyName, PropertyConventionContext context)
        {
            if (propertyName == "TestProperty")
            {
                context.IsIgnored = true;
            }
        }
    }

    public class LowPriorityEntityConvention : IEntityConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType)
        {
            return entityType.Name.Contains("Applicable");
        }

        public void Configure(Type entityType, EntityConventionContext context)
        {
            context.EnableDirectDeletion = true;
        }
    }

    public class HighPriorityEntityConvention : IEntityConvention
    {
        public int Priority => 1000;

        public bool CanApply(Type entityType)
        {
            return entityType.Name.Contains("Applicable");
        }

        public void Configure(Type entityType, EntityConventionContext context)
        {
            context.EnableDirectDeletion = false;
        }
    }

    // Test entities
    public class TestApplicableEntity : Entity
    {
        public string TestProperty { get; set; }
        public string OtherProperty { get; set; }
    }

    public class TestNonApplicableEntity : Entity
    {
        public string NonTestProperty { get; set; }
    }

    public class TestSoftDeletableEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class TestAuditableEntity : Entity, IHasCreatedAt
    {
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}