using MiCake.Core;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Tests.Fakes.Aggregates;
using MiCake.DDD.Tests.Fakes.Entities;
using MiCake.DDD.Tests.Fakes.StorageModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MiCake.DDD.Tests.Metadata
{
    //Do not add domain objects in the assembly at will this operation will affect all assertions
    public class DomainMetadata_Tests : MiCakeDDDTestsBase
    {
        public DomainMetadata_Tests() : base()
        {
        }

        [Fact]
        public void GetDomainMetadata_AppointAsm_ShouldNotNull()
        {
            BuildServiceCollection();

            Assembly[] assemblies = { GetType().Assembly };
            Services.Configure<MiCakeApplicationOptions>(options => options.DomianLayerAssemblies = assemblies);

            var provider = Services.BuildServiceProvider();
            var domainMetadata = provider.GetService<DomainMetadata>();

            Assert.NotNull(domainMetadata);
        }

        [Fact]
        public void GetDomainMetadata_NotAppointAsm_ShouldNotNull()
        {
            BuildServiceCollection();

            var provider = Services.BuildServiceProvider();
            var domainMetadata = provider.GetService<DomainMetadata>();

            Assert.NotNull(domainMetadata);
            Assert.Equal(domainMetadata.DomainLayerAssembly[0], GetType().Assembly);
        }

        [Fact]
        public void DomainObjectModelProvider_ShouldRightExecutionSequence()
        {
            BuildServiceCollection();

            Services.AddSingleton<TestModelProviderRecorder>();
            Services.AddTransient<IDomainObjectModelProvider, TestDomainObjectModelProvider>();
            Services.AddTransient<IDomainObjectModelProvider, TestDomainObjectModelProvider_2>();

            var provider = Services.BuildServiceProvider();
            provider.GetService<DomainMetadata>();

            var recorder = provider.GetService<TestModelProviderRecorder>();

            Assert.NotNull(recorder);

            Assert.Equal(4, recorder.ModelProviderInfo.Count);

            var firstRecordInfo = "TestDomainObjectModelProviderOnProvidersExecuting";
            Assert.Equal(firstRecordInfo, recorder.ModelProviderInfo.First().Trim());

            var lastRecordInfo = "TestDomainObjectModelProviderOnProvidersExecuted";
            Assert.Equal(lastRecordInfo, recorder.ModelProviderInfo.Last().Trim());
        }

        [Fact]
        public void AggregateRootDescriptor_ShouldSetRightPOModel()
        {
            var aggregateRootDescriptor = new AggregateRootDescriptor(typeof(HasStorageModelAggregateRoot));

            Assert.Throws<ArgumentException>(() =>
            {
                aggregateRootDescriptor.SetStorageModel(typeof(EntityA));
            });

            Assert.Throws<ArgumentException>(() =>
            {
                aggregateRootDescriptor.SetStorageModel(typeof(WrongPOModel));
            });

            aggregateRootDescriptor.SetStorageModel(typeof(DemoStorageModel));
            Assert.Equal(typeof(DemoStorageModel), aggregateRootDescriptor.StorageModel);
        }

        [Fact]
        public void DomainMetadata_ShouldRightInfo()
        {
            BuildServiceCollection();

            Assembly[] assemblies = { GetType().Assembly };
            Services.Configure<MiCakeApplicationOptions>(options => options.DomianLayerAssemblies = assemblies);

            var provider = Services.BuildServiceProvider();
            var metadata = provider.GetService<DomainMetadata>();

            var entitiesCount = metadata.DomainObject.Entities.Count;
            Assert.Equal(7, entitiesCount);

            Assert.Equal(2, metadata.DomainObject.AggregateRoots.Count);

            var entityDesc = metadata.DomainObject.Entities.FirstOrDefault(s => s.Type.Equals(typeof(EntityA)));
            Assert.NotNull(entityDesc);
            Assert.Equal(typeof(int), entityDesc.PrimaryKey);

            var inheritEntityDesc = metadata.DomainObject.Entities.FirstOrDefault(s => s.Type.Equals(typeof(ClassAInheritGenericEntityA)));
            Assert.NotNull(inheritEntityDesc);
            Assert.Equal(typeof(Guid), inheritEntityDesc.PrimaryKey);

            var hasPOAggregate = metadata.DomainObject.AggregateRoots.FirstOrDefault(s => s.Type.Equals(typeof(HasStorageModelAggregateRoot)));
            Assert.NotNull(hasPOAggregate);
            Assert.Equal(typeof(DemoStorageModel), hasPOAggregate.StorageModel);
            Assert.Equal(typeof(Guid), hasPOAggregate.PrimaryKey);
        }
    }
}