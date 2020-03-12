using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Tests.Fakes.StorageModels;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MiCake.DDD.Tests
{
    public class DomainMetadataCreator_Tests
    {
        public IMiCakeModuleCollection MiCakeModules { get; set; }

        public DomainMetadataCreator_Tests()
        {
            MiCakeModules = new MiCakeModuleCollection()
            {
                new MiCakeModuleDescriptor(typeof(DDDTestModule),(MiCakeModule)Activator.CreateInstance(typeof(DDDTestModule)))
            };
        }

        [Fact]
        public void SpecifyAssembly_ShouldOnlyLoadTheseAsm()
        {
            Assembly[] specifyAsm = new Assembly[1] { typeof(DDDTestModule).Assembly };
            DomainMetadata result;

            using (var creator = new DomainMetadataCreator(MiCakeModules, specifyAsm))
            {
                result = creator.Create();
            }

            Assert.NotNull(specifyAsm);
            Assert.Single(result.DomainLayerAssembly);
        }

        [Fact]
        public void NotSpecifyAssembly_ShouldFindAsm()
        {
            DomainMetadata result;

            using (var creator = new DomainMetadataCreator(MiCakeModules))
            {
                result = creator.Create();
            }

            Assert.Single(result.DomainLayerAssembly);
        }

        [Fact]
        public void HasAggregateRootProvider_ShouldFindAggregate()
        {
            Assembly[] specifyAsm = new Assembly[1] { typeof(DDDTestModule).Assembly };
            DomainMetadata result;

            using (var creator = new DomainMetadataCreator(MiCakeModules, specifyAsm))
            {
                creator.AddDescriptorProvider(new AggregateRootDescriptorProvider(MiCakeModules));

                result = creator.Create();
            }

            Assert.Empty(result.Entities);

            var storageModelAggrageteRoot = result.AggregateRoots.FirstOrDefault(s => s.Type == typeof(HasStorageModelAggregateRoot));
            Assert.NotNull(storageModelAggrageteRoot);

            Assert.NotNull(storageModelAggrageteRoot.StorageModel);
        }

        [Fact]
        public void HasEntityProvider_ShouldFindEntity()
        {
            DomainMetadata result;

            using (var creator = new DomainMetadataCreator(MiCakeModules))
            {
                creator.AddDescriptorProvider(new EntityDescriptorProvider());

                result = creator.Create();
            }

            Assert.NotEmpty(result.Entities);
            Assert.Equal(8, result.Entities.Count);
        }
    }
}
