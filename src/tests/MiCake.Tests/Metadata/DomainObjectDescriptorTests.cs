using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Metadata;
using System;
using System.Collections.Generic;
using Xunit;

namespace MiCake.Tests.Metadata
{
    /// <summary>
    /// Unit tests for DomainTypeDescriptor and its derived classes
    /// Tests descriptor creation, properties, and type safety
    /// </summary>
    public class DomainTypeDescriptorTests
    {
        #region DomainTypeDescriptor Base Class Tests

        [Fact]
        public void DomainTypeDescriptor_Constructor_WithNullType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestDescriptor(null));
        }

        [Fact]
        public void DomainTypeDescriptor_Constructor_WithValidType_ShouldSetProperties()
        {
            // Arrange
            var type = typeof(TestEntity);

            // Act
            var descriptor = new TestDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.Type);
            Assert.Equal(type.Name, descriptor.Name);
        }

        #endregion

        #region EntityDescriptor Tests

        [Fact]
        public void EntityDescriptor_Constructor_WithNullEntityType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EntityDescriptor(null, typeof(int)));
        }

        [Fact]
        public void EntityDescriptor_Constructor_WithNullKeyType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EntityDescriptor(typeof(TestEntity), null));
        }

        [Fact]
        public void EntityDescriptor_Constructor_WithValidParameters_ShouldSetProperties()
        {
            // Arrange
            var entityType = typeof(TestEntity);
            var keyType = typeof(int);

            // Act
            var descriptor = new EntityDescriptor(entityType, keyType);

            // Assert
            Assert.Equal(entityType, descriptor.Type);
            Assert.Equal(keyType, descriptor.KeyType);
            Assert.Equal(entityType.Name, descriptor.Name);
        }

        #endregion

        #region AggregateRootDescriptor Tests

        [Fact]
        public void AggregateRootDescriptor_Constructor_WithValidParameters_ShouldSetProperties()
        {
            // Arrange
            var aggregateType = typeof(TestAggregate);
            var keyType = typeof(int);

            // Act
            var descriptor = new AggregateRootDescriptor(aggregateType, keyType);

            // Assert
            Assert.Equal(aggregateType, descriptor.Type);
            Assert.Equal(keyType, descriptor.KeyType);
            Assert.Equal(aggregateType.Name, descriptor.Name);
        }

        [Fact]
        public void AggregateRootDescriptor_InheritsFromEntityDescriptor_ShouldHaveKeyType()
        {
            // Arrange
            var aggregateType = typeof(TestAggregate);
            var keyType = typeof(int);

            // Act
            var descriptor = new AggregateRootDescriptor(aggregateType, keyType);

            // Assert
            Assert.IsType<AggregateRootDescriptor>(descriptor);
            Assert.IsAssignableFrom<EntityDescriptor>(descriptor);
            Assert.Equal(keyType, descriptor.KeyType);
        }

        #endregion

        #region ValueObjectDescriptor Tests

        [Fact]
        public void ValueObjectDescriptor_Constructor_WithNullValueObjectType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValueObjectDescriptor(null));
        }

        [Fact]
        public void ValueObjectDescriptor_Constructor_WithValidType_ShouldSetProperties()
        {
            // Arrange
            var valueObjectType = typeof(TestValueObject);

            // Act
            var descriptor = new ValueObjectDescriptor(valueObjectType);

            // Assert
            Assert.Equal(valueObjectType, descriptor.Type);
            Assert.Equal(valueObjectType.Name, descriptor.Name);
        }

        #endregion

        #region Type Safety Tests

        [Fact]
        public void Descriptors_ShouldBeAssignableToBaseType()
        {
            // Arrange
            var entityDesc = new EntityDescriptor(typeof(TestEntity), typeof(int));
            var aggregateDesc = new AggregateRootDescriptor(typeof(TestAggregate), typeof(int));
            var valueObjectDesc = new ValueObjectDescriptor(typeof(TestValueObject));

            // Act & Assert
            Assert.IsAssignableFrom<DomainTypeDescriptor>(entityDesc);
            Assert.IsAssignableFrom<DomainTypeDescriptor>(aggregateDesc);
            Assert.IsAssignableFrom<DomainTypeDescriptor>(valueObjectDesc);
        }

        [Fact]
        public void AggregateRootDescriptor_ShouldBeAssignableToEntityDescriptor()
        {
            // Arrange
            var aggregateDesc = new AggregateRootDescriptor(typeof(TestAggregate), typeof(int));

            // Act & Assert
            Assert.IsAssignableFrom<EntityDescriptor>(aggregateDesc);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public void IntegrationScenario_DomainModelDescriptors_ShouldWorkTogether()
        {
            // Arrange: Create descriptors for a complete domain model
            var userAggregate = new AggregateRootDescriptor(typeof(User), typeof(int));
            var orderEntity = new EntityDescriptor(typeof(Order), typeof(int));
            var addressValueObject = new ValueObjectDescriptor(typeof(Address));

            // Act & Assert: Verify all descriptors work correctly
            Assert.Equal(typeof(User), userAggregate.Type);
            Assert.Equal(typeof(int), userAggregate.KeyType);

            Assert.Equal(typeof(Order), orderEntity.Type);
            Assert.Equal(typeof(int), orderEntity.KeyType);

            Assert.Equal(typeof(Address), addressValueObject.Type);
        }

        [Fact]
        public void IntegrationScenario_TypeHierarchy_ShouldBeCorrect()
        {
            // Arrange
            var descriptors = new List<DomainTypeDescriptor>
            {
                new AggregateRootDescriptor(typeof(User), typeof(int)),
                new EntityDescriptor(typeof(Order), typeof(int)),
                new ValueObjectDescriptor(typeof(Address))
            };

            // Act & Assert: Check type hierarchy
            var aggregateDesc = descriptors[0] as AggregateRootDescriptor;
            var entityDesc = descriptors[1] as EntityDescriptor;
            var valueObjectDesc = descriptors[2] as ValueObjectDescriptor;

            Assert.NotNull(aggregateDesc);
            Assert.NotNull(entityDesc);
            Assert.NotNull(valueObjectDesc);

            // Aggregate root is also an entity
            Assert.IsAssignableFrom<EntityDescriptor>(aggregateDesc);
        }

        #endregion

        #region Test Classes

        // Test descriptor for base class testing
        private class TestDescriptor : DomainTypeDescriptor
        {
            public TestDescriptor(Type type) : base(type) { }
        }

        public class User : AggregateRoot<int>
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public class Order : Entity<int>
        {
            public string OrderNumber { get; set; }
            public decimal Total { get; set; }
        }

        public class Address : ValueObject
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string ZipCode { get; set; }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Street;
                yield return City;
                yield return ZipCode;
            }
        }

        public class TestEntity : Entity<int>
        {
            public string Name { get; set; }
        }

        public class TestAggregate : AggregateRoot<int>
        {
            public string Title { get; set; }
        }

        public class TestValueObject : ValueObject
        {
            public string Value { get; set; }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Value;
            }
        }

        #endregion
    }
}