using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using System;
using System.Linq.Expressions;
using Xunit;

namespace MiCake.Tests.Store.NewConventions
{
    public class SoftDeletionConventionTests
    {
        private readonly SoftDeletionConvention _convention;
        
        public SoftDeletionConventionTests()
        {
            _convention = new SoftDeletionConvention();
        }
        
        [Fact]
        public void Priority_ShouldBe100()
        {
            // Act & Assert
            Assert.Equal(100, _convention.Priority);
        }
        
        [Fact]
        public void CanApply_WithISoftDeletionEntity_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            
            // Act
            var result = _convention.CanApply(entityType);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void CanApply_WithNonISoftDeletionEntity_ShouldReturnFalse()
        {
            // Arrange
            var entityType = typeof(RegularEntity);
            
            // Act
            var result = _convention.CanApply(entityType);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void CanApply_WithAbstractISoftDeletionEntity_ShouldReturnTrue()
        {
            // Arrange
            var entityType = typeof(AbstractSoftDeletableEntity);
            
            // Act
            var result = _convention.CanApply(entityType);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void Configure_WithISoftDeletionEntity_ShouldSetCorrectFlags()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            var context = new EntityConventionContext();
            
            // Act
            _convention.Configure(entityType, context);
            
            // Assert
            Assert.True(context.EnableSoftDeletion);
            Assert.False(context.EnableDirectDeletion);
        }
        
        [Fact]
        public void Configure_WithISoftDeletionEntity_ShouldCreateQueryFilter()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            var context = new EntityConventionContext();
            
            // Act
            _convention.Configure(entityType, context);
            
            // Assert
            Assert.NotNull(context.QueryFilter);
            Assert.True(context.QueryFilter is LambdaExpression);
        }
        
        [Fact]
        public void Configure_QueryFilter_ShouldFilterNonDeletedEntities()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            var context = new EntityConventionContext();
            
            // Act
            _convention.Configure(entityType, context);
            
            // Assert
            var queryFilter = context.QueryFilter;
            Assert.NotNull(queryFilter);
            
            // Verify the lambda expression structure
            Assert.Single(queryFilter.Parameters);
            Assert.Equal(entityType, queryFilter.Parameters[0].Type);
            Assert.Equal(typeof(bool), queryFilter.ReturnType);
            
            // The expression should be: entity => !entity.IsDeleted
            var body = queryFilter.Body;
            Assert.IsType<UnaryExpression>(body);
            var unaryExpression = (UnaryExpression)body;
            Assert.Equal(ExpressionType.Not, unaryExpression.NodeType);
        }
        
        [Fact]
        public void Configure_QueryFilter_ShouldCompileAndWork()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            var context = new EntityConventionContext();
            _convention.Configure(entityType, context);
            
            // Act
            var compiledFilter = context.QueryFilter.Compile();
            var activeEntity = new SoftDeletableEntity { IsDeleted = false };
            var deletedEntity = new SoftDeletableEntity { IsDeleted = true };
            
            // Assert
            Assert.True((bool)compiledFilter.DynamicInvoke(activeEntity));
            Assert.False((bool)compiledFilter.DynamicInvoke(deletedEntity));
        }
        
        [Fact]
        public void Configure_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var entityType = typeof(SoftDeletableEntity);
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _convention.Configure(entityType, null));
        }
        
        [Fact]
        public void Configure_WithNullEntityType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new EntityConventionContext();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _convention.Configure(null, context));
        }
        
        [Fact]
        public void Configure_WithDerivedISoftDeletionEntity_ShouldWork()
        {
            // Arrange
            var entityType = typeof(DerivedSoftDeletableEntity);
            var context = new EntityConventionContext();
            
            // Act
            _convention.Configure(entityType, context);
            
            // Assert
            Assert.True(context.EnableSoftDeletion);
            Assert.False(context.EnableDirectDeletion);
            Assert.NotNull(context.QueryFilter);
        }
        
        [Fact]
        public void Configure_QueryFilter_WithDerivedEntity_ShouldCompileCorrectly()
        {
            // Arrange
            var entityType = typeof(DerivedSoftDeletableEntity);
            var context = new EntityConventionContext();
            _convention.Configure(entityType, context);
            
            // Act
            var compiledFilter = context.QueryFilter.Compile();
            var activeEntity = new DerivedSoftDeletableEntity { IsDeleted = false, Category = "Test" };
            var deletedEntity = new DerivedSoftDeletableEntity { IsDeleted = true, Category = "Test" };
            
            // Assert
            Assert.True((bool)compiledFilter.DynamicInvoke(activeEntity));
            Assert.False((bool)compiledFilter.DynamicInvoke(deletedEntity));
        }
    }
    
    // Test entities for soft deletion convention testing
    public class SoftDeletableEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public abstract class AbstractSoftDeletableEntity : Entity, ISoftDeletion
    {
        public string Title { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class DerivedSoftDeletableEntity : SoftDeletableEntity
    {
        public string Category { get; set; }
    }
}