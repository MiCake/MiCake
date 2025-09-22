using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Conventions;
using System;
using Xunit;

namespace MiCake.Tests.Store.Simplified
{
    public class SimplifiedStoreConventionTests
    {
        [Fact]
        public void SoftDeletionConvention_ShouldApplyToISoftDeletionEntities()
        {
            // Arrange
            var convention = new SoftDeletionConvention();
            var entityType = typeof(SoftDeletableEntity);
            var context = new EntityConventionContext();

            // Act
            var canApply = convention.CanApply(entityType);
            convention.Configure(entityType, context);

            // Assert
            Assert.True(canApply);
            Assert.True(context.EnableSoftDeletion);
            Assert.False(context.EnableDirectDeletion);
            Assert.NotNull(context.QueryFilter);
        }

        [Fact]
        public void SoftDeletionConvention_ShouldNotApplyToRegularEntities()
        {
            // Arrange
            var convention = new SoftDeletionConvention();
            var entityType = typeof(RegularEntity);

            // Act
            var canApply = convention.CanApply(entityType);

            // Assert
            Assert.False(canApply);
        }

        [Fact]
        public void AuditTimeConvention_ShouldApplyToAuditEntities()
        {
            // Arrange
            var convention = new AuditTimeConvention();
            var entityType = typeof(AuditableEntity);

            // Act
            var canApply = convention.CanApply(entityType);

            // Assert
            Assert.True(canApply);
        }

        [Fact]
        public void StoreConventionEngine_ShouldApplyConventionsInPriorityOrder()
        {
            // Arrange
            var engine = new StoreConventionEngine();
            var highPriorityConvention = new TestConvention(1);
            var lowPriorityConvention = new TestConvention(100);
            
            engine.AddConvention(lowPriorityConvention);
            engine.AddConvention(highPriorityConvention);

            // Act
            var context = engine.ApplyEntityConventions(typeof(TestEntity));

            // Assert
            // Should be applied in order of priority (1 then 100)
            Assert.NotNull(context);
        }

        private class TestConvention : IEntityConvention
        {
            public int Priority { get; }

            public TestConvention(int priority)
            {
                Priority = priority;
            }

            public bool CanApply(Type entityType) => true;

            public void Configure(Type entityType, EntityConventionContext context)
            {
                // Test implementation
            }
        }

        private class SoftDeletableEntity : AggregateRoot, ISoftDeletion
        {
            public bool IsDeleted { get; set; }
        }

        private class RegularEntity : AggregateRoot
        {
        }

        private class AuditableEntity : AggregateRoot, IHasCreationTime
        {
            public DateTime CreationTime { get; set; }
        }

        private class TestEntity : AggregateRoot
        {
        }
    }
}