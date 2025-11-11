using MiCake.Core;
using MiCake.DDD.Infrastructure.Metadata;
using Microsoft.Extensions.DependencyInjection;
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
            Services.Configure<MiCakeApplicationOptions>(options => options.DomainLayerAssemblies = assemblies);

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
    }
}