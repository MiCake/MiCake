using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Metadata;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Metadata
{
    /// <summary>
    /// Unit tests for DomainMetadataProvider functionality
    /// Tests assembly scanning, caching, and domain object discovery
    /// </summary>
    public class DomainMetadataProviderTests
    {
        private readonly Mock<IMiCakeModuleContext> _mockModuleContext;
        private readonly Mock<IOptions<MiCakeApplicationOptions>> _mockAppOptions;

        public DomainMetadataProviderTests()
        {
            _mockModuleContext = new Mock<IMiCakeModuleContext>();
            _mockAppOptions = new Mock<IOptions<MiCakeApplicationOptions>>();

            // Set up default empty module collection
            var emptyCollection = new MiCakeModuleCollection();
            _mockModuleContext.Setup(c => c.MiCakeModules).Returns(emptyCollection);

            // Set up default app options
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions());
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullModuleContext_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadataProvider(null, _mockAppOptions.Object));
        }

        [Fact]
        public void Constructor_WithNullAppOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DomainMetadataProvider(_mockModuleContext.Object, null));
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeProvider()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            // Act
            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region GetDomainMetadata Tests

        [Fact]
        public void GetDomainMetadata_FirstCall_ShouldScanAndCacheMetadata()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Act
            var metadata = provider.GetDomainMetadata();

            // Assert
            Assert.NotNull(metadata);
            Assert.Contains(typeof(DomainMetadataProviderTests).Assembly, metadata.Assemblies);
        }

        [Fact]
        public void GetDomainMetadata_SecondCall_ShouldReturnCachedMetadata()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Act
            var metadata1 = provider.GetDomainMetadata();
            var metadata2 = provider.GetDomainMetadata();

            // Assert
            Assert.Same(metadata1, metadata2);
        }

        [Fact]
        public async Task GetDomainMetadata_ConcurrentCalls_ShouldReturnSameInstance()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Act
            DomainMetadata metadata1 = null;
            DomainMetadata metadata2 = null;

            var task1 = Task.Run(() => metadata1 = provider.GetDomainMetadata());
            var task2 = Task.Run(() => metadata2 = provider.GetDomainMetadata());

            await Task.WhenAll(task1, task2);

            // Assert
            Assert.NotNull(metadata1);
            Assert.NotNull(metadata2);
            Assert.Same(metadata1, metadata2);
        }

        #endregion

        #region Assembly Discovery Tests

        [Fact]
        public void Constructor_WithoutDomainAssemblies_ShouldDiscoverFromModules()
        {
            // Arrange
            var testModule = new TestModule();
            var moduleDescriptor = new MiCakeModuleDescriptor(typeof(TestModule), testModule);
            var moduleCollection = new MiCakeModuleCollection();
            moduleCollection.Add(moduleDescriptor);

            _mockModuleContext.Setup(c => c.MiCakeModules).Returns(moduleCollection);

            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions());

            // Act
            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region Domain Object Discovery Tests

        [Fact]
        public void GetDomainMetadata_ShouldDiscoverTestDomainObjects()
        {
            // Arrange
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Act
            var metadata = provider.GetDomainMetadata();

            // Assert
            Assert.NotNull(metadata);
            Assert.Contains(typeof(DomainMetadataProviderTests).Assembly, metadata.Assemblies);

            // Should find our test domain objects
            var aggregate = metadata.GetDescriptor<AggregateRootDescriptor>(typeof(TestAggregate));
            var entity = metadata.GetDescriptor<EntityDescriptor>(typeof(TestEntity));
            var valueObject = metadata.GetDescriptor<ValueObjectDescriptor>(typeof(TestValueObject));

            Assert.NotNull(aggregate);
            Assert.NotNull(entity);
            Assert.NotNull(valueObject);
        }

        [Fact]
        public void GetDomainMetadata_WithEmptyAssemblies_ShouldReturnEmptyMetadata()
        {
            // Arrange
            var mockModuleContext = new Mock<IMiCakeModuleContext>();
            mockModuleContext.Setup(c => c.MiCakeModules).Returns(new MiCakeModuleCollection());

            var appOptions = Options.Create(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = Array.Empty<Assembly>()
            });

            var provider = new DomainMetadataProvider(mockModuleContext.Object, appOptions);

            // Act
            var metadata = provider.GetDomainMetadata();

            // Assert
            Assert.NotNull(metadata);
            Assert.Empty(metadata.AggregateRoots);
            Assert.Empty(metadata.Entities);
            Assert.Empty(metadata.ValueObjects);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public void IntegrationScenario_FullApplicationStartup_ShouldProvideCompleteMetadata()
        {
            // Arrange: Simulate a full application with multiple domain objects
            var assemblies = new[] { typeof(DomainMetadataProviderTests).Assembly };
            _mockAppOptions.Setup(o => o.Value).Returns(new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            });

            var provider = new DomainMetadataProvider(_mockModuleContext.Object, _mockAppOptions.Object);

            // Act: Get metadata multiple times (simulating application usage)
            var metadata1 = provider.GetDomainMetadata();
            var metadata2 = provider.GetDomainMetadata();

            // Assert: Metadata is consistent and complete
            Assert.Same(metadata1, metadata2);
            Assert.True(metadata1.IsDomainObject(typeof(TestAggregate)));
            Assert.True(metadata1.IsDomainObject(typeof(TestEntity)));
            Assert.True(metadata1.IsDomainObject(typeof(TestValueObject)));

            // Verify descriptors are correct
            var aggregateDesc = metadata1.GetDescriptor<AggregateRootDescriptor>(typeof(TestAggregate));
            Assert.Equal(typeof(int), aggregateDesc.KeyType);

            var entityDesc = metadata1.GetDescriptor<EntityDescriptor>(typeof(TestEntity));
            Assert.Equal(typeof(int), entityDesc.KeyType);
        }

        #endregion

        #region Test Classes

        public class TestModule : MiCakeModule
        {
            public override void ConfigureServices(ModuleConfigServiceContext context)
            {
                // No services to configure for test
            }
        }

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