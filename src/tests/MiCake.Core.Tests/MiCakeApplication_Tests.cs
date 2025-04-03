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
        public async Task ApplicationOptions_AppointOptions_ShouldNotNull()
        {
            Assembly[] assemblies = { GetType().Assembly };
            MiCakeApplicationOptions options = new()
            {
                DomainLayerAssemblies = assemblies
            };

            MiCakeApplication miCakeApplication = new(Services, options, false);
            Services.AddSingleton<IMiCakeApplication>(miCakeApplication);
            miCakeApplication.SetEntry(typeof(MiCakeCoreTestModule));
            await miCakeApplication.Initialize();

            var resolvedOptions = Services.BuildServiceProvider().GetService<IOptions<MiCakeApplicationOptions>>();

            Assert.NotNull(resolvedOptions);
        }
    }
}
