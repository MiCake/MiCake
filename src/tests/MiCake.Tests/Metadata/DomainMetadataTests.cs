using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Infrastructure.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MiCake.Tests.Metadata
{
    /// <summary>
    /// Unit tests for DomainMetadata functionality
    /// Tests domain object discovery, caching, and lookup operations
    /// </summary>
    public class DomainMetadataTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateMetadata()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor>();
            var valueObjects = new List<ValueObjectDescriptor>();

            // Act
            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(assemblies, metadata.Assemblies);
            Assert.Empty(metadata.AggregateRoots);
            Assert.Empty(metadata.Entities);
            Assert.Empty(metadata.ValueObjects);
        }

        [Fact]
        public void Constructor_WithNullAssemblies_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadata(null, new List<AggregateRootDescriptor>(), new List<EntityDescriptor>(), new List<ValueObjectDescriptor>()));
        }

        [Fact]
        public void Constructor_WithNullAggregates_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadata(new[] { typeof(DomainMetadataTests).Assembly }, null, new List<EntityDescriptor>(), new List<ValueObjectDescriptor>()));
        }

        [Fact]
        public void Constructor_WithNullEntities_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadata(new[] { typeof(DomainMetadataTests).Assembly }, new List<AggregateRootDescriptor>(), null, new List<ValueObjectDescriptor>()));
        }

        [Fact]
        public void Constructor_WithNullValueObjects_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadata(new[] { typeof(DomainMetadataTests).Assembly }, new List<AggregateRootDescriptor>(), new List<EntityDescriptor>(), null));
        }

        #endregion

        #region GetDescriptor Tests

        [Fact]
        public void GetDescriptor_WithRegisteredType_ShouldReturnDescriptor()
        {
            // Arrange
            var testEntityType = typeof(TestEntity);
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor> { new EntityDescriptor(testEntityType, typeof(int)) };
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var descriptor = metadata.GetDescriptor(testEntityType);

            // Assert
            Assert.NotNull(descriptor);
            Assert.IsType<EntityDescriptor>(descriptor);
            Assert.Equal(testEntityType, descriptor.Type);
        }

        [Fact]
        public void GetDescriptor_WithUnregisteredType_ShouldReturnNull()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor>();
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var descriptor = metadata.GetDescriptor(typeof(string));

            // Assert
            Assert.Null(descriptor);
        }

        [Fact]
        public void GetDescriptor_Generic_WithCorrectType_ShouldReturnTypedDescriptor()
        {
            // Arrange
            var testEntityType = typeof(TestEntity);
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor> { new EntityDescriptor(testEntityType, typeof(int)) };
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var descriptor = metadata.GetDescriptor<EntityDescriptor>(testEntityType);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(testEntityType, descriptor.Type);
            Assert.Equal(typeof(int), descriptor.KeyType);
        }

        [Fact]
        public void GetDescriptor_Generic_WithWrongType_ShouldReturnNull()
        {
            // Arrange
            var testEntityType = typeof(TestEntity);
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor> { new EntityDescriptor(testEntityType, typeof(int)) };
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var descriptor = metadata.GetDescriptor<AggregateRootDescriptor>(testEntityType);

            // Assert
            Assert.Null(descriptor);
        }

        #endregion

        #region IsDomainObject Tests

        [Fact]
        public void IsDomainObject_WithRegisteredType_ShouldReturnTrue()
        {
            // Arrange
            var testEntityType = typeof(TestEntity);
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor> { new EntityDescriptor(testEntityType, typeof(int)) };
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var result = metadata.IsDomainObject(testEntityType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDomainObject_WithUnregisteredType_ShouldReturnFalse()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor>();
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act
            var result = metadata.IsDomainObject(typeof(string));

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public void IntegrationScenario_CompleteDomainModel_ShouldProvideCorrectMetadata()
        {
            // Arrange: Create a complete domain model scenario
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>
            {
                new AggregateRootDescriptor(typeof(TestAggregate), typeof(int))
            };
            var entities = new List<EntityDescriptor>
            {
                new EntityDescriptor(typeof(TestEntity), typeof(int)),
                new EntityDescriptor(typeof(TestAggregate), typeof(int)) // Aggregate roots are also entities
            };
            var valueObjects = new List<ValueObjectDescriptor>
            {
                new ValueObjectDescriptor(typeof(TestValueObject))
            };

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act & Assert: Verify all lookups work correctly
            Assert.True(metadata.IsDomainObject(typeof(TestAggregate)));
            Assert.True(metadata.IsDomainObject(typeof(TestEntity)));
            Assert.True(metadata.IsDomainObject(typeof(TestValueObject)));

            var aggregateDesc = metadata.GetDescriptor<AggregateRootDescriptor>(typeof(TestAggregate));
            Assert.NotNull(aggregateDesc);
            Assert.Equal(typeof(int), aggregateDesc.KeyType);

            var entityDesc = metadata.GetDescriptor<EntityDescriptor>(typeof(TestEntity));
            Assert.NotNull(entityDesc);
            Assert.Equal(typeof(int), entityDesc.KeyType);

            var valueObjectDesc = metadata.GetDescriptor<ValueObjectDescriptor>(typeof(TestValueObject));
            Assert.NotNull(valueObjectDesc);
        }

        [Fact]
        public void IntegrationScenario_CachePerformance_ShouldReuseDescriptors()
        {
            // Arrange
            var testEntityType = typeof(TestEntity);
            var assemblies = new[] { typeof(DomainMetadataTests).Assembly };
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor> { new EntityDescriptor(testEntityType, typeof(int)) };
            var valueObjects = new List<ValueObjectDescriptor>();

            var metadata = new DomainMetadata(assemblies, aggregates, entities, valueObjects);

            // Act: Multiple lookups of same type
            var desc1 = metadata.GetDescriptor(testEntityType);
            var desc2 = metadata.GetDescriptor(testEntityType);
            var desc3 = metadata.GetDescriptor<EntityDescriptor>(testEntityType);

            // Assert: Same instance returned (reference equality)
            Assert.Same(desc1, desc2);
            Assert.Same(desc1, desc3);
        }

        #endregion

        #region Test Classes

        public class TestAggregate : AggregateRoot<int>
        {
            public string Name { get; set; }
        }

        public class TestEntity : Entity<int>
        {
            public string Description { get; set; }
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