using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using System;
using System.Linq.Expressions;
using Xunit;

namespace MiCake.Tests.Store.NewConventions
{
    public class StoreConventionTests
    {
        [Fact]
        public void IStoreConvention_ShouldHaveCorrectInterfaceMembers()
        {
            // Arrange & Act
            var conventionType = typeof(IStoreConvention);
            
            // Assert
            Assert.True(conventionType.IsInterface);
            Assert.NotNull(conventionType.GetProperty("Priority"));
            Assert.NotNull(conventionType.GetMethod("CanApply"));
        }
        
        [Fact]
        public void IEntityConvention_ShouldInheritFromIStoreConvention()
        {
            // Arrange & Act
            var entityConventionType = typeof(IEntityConvention);
            
            // Assert
            Assert.True(typeof(IStoreConvention).IsAssignableFrom(entityConventionType));
            Assert.NotNull(entityConventionType.GetMethod("Configure"));
        }
        
        [Fact]
        public void IPropertyConvention_ShouldInheritFromIStoreConvention()
        {
            // Arrange & Act
            var propertyConventionType = typeof(IPropertyConvention);
            
            // Assert
            Assert.True(typeof(IStoreConvention).IsAssignableFrom(propertyConventionType));
            Assert.NotNull(propertyConventionType.GetMethod("Configure"));
        }
        
        [Fact]
        public void EntityConventionContext_ShouldHaveCorrectProperties()
        {
            // Arrange & Act
            var context = new EntityConventionContext();
            
            // Assert
            Assert.False(context.EnableSoftDeletion);
            Assert.True(context.EnableDirectDeletion);
            Assert.Null(context.QueryFilter);
        }
        
        [Fact]
        public void PropertyConventionContext_ShouldHaveCorrectProperties()
        {
            // Arrange & Act
            var context = new PropertyConventionContext();
            
            // Assert
            Assert.False(context.IsIgnored);
            Assert.False(context.HasDefaultValue);
            Assert.Null(context.DefaultValue);
        }
        
        [Fact]
        public void EntityConventionContext_QueryFilter_ShouldAcceptLambdaExpression()
        {
            // Arrange
            var context = new EntityConventionContext();
            var parameter = Expression.Parameter(typeof(TestEntity), "entity");
            var property = Expression.Property(parameter, nameof(TestEntity.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);
            
            // Act
            context.QueryFilter = lambda;
            
            // Assert
            Assert.NotNull(context.QueryFilter);
            Assert.Equal(lambda, context.QueryFilter);
        }
        
        [Fact]
        public void EntityConventionContext_SoftDeletion_ShouldSetCorrectFlags()
        {
            // Arrange
            var context = new EntityConventionContext();
            
            // Act
            context.EnableSoftDeletion = true;
            context.EnableDirectDeletion = false;
            
            // Assert
            Assert.True(context.EnableSoftDeletion);
            Assert.False(context.EnableDirectDeletion);
        }
        
        [Fact]
        public void PropertyConventionContext_PropertyConfiguration_ShouldWork()
        {
            // Arrange
            var context = new PropertyConventionContext();
            var defaultValue = DateTime.UtcNow;
            
            // Act
            context.IsIgnored = true;
            context.HasDefaultValue = true;
            context.DefaultValue = defaultValue;
            
            // Assert
            Assert.True(context.IsIgnored);
            Assert.True(context.HasDefaultValue);
            Assert.Equal(defaultValue, context.DefaultValue);
        }
    }
    
    // Test entities for convention testing
    public class TestEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class AuditableEntity : Entity, IHasCreationTime, IHasModificationTime
    {
        public string Title { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
    }
    
    public class RegularEntity : Entity
    {
        public string Description { get; set; }
    }
}