using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Core.Tests
{
    public class MiCakeApplication_Tests
    {
        public IServiceCollection Services { get; set; } = new ServiceCollection();

        public MiCakeApplication_Tests()
        {
            Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        }

        [Fact]
        public void ApplicationOptions_AppointOptions_ShouldNotNull()
        {
            Assembly[] assemblies = { GetType().Assembly };
            MiCakeApplicationOptions options = new()
            {
                DomainLayerAssemblies = assemblies
            };

            // Use builder to configure
            var builder = new MiCakeBuilder(Services, typeof(MiCakeCoreTestModule), options);
            builder.Build();

            var serviceProvider = Services.BuildServiceProvider();
            var resolvedOptions = serviceProvider.GetService<IOptions<MiCakeApplicationOptions>>();

            Assert.NotNull(resolvedOptions);
        }
    }
}
