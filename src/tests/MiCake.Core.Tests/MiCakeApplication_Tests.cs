using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
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
            MiCakeApplicationOptions options = new MiCakeApplicationOptions()
            {
                DomainLayerAssemblies = assemblies
            };

            MiCakeApplication miCakeApplication = new MiCakeApplication(Services, options, false);
            Services.AddSingleton<IMiCakeApplication>(miCakeApplication);
            miCakeApplication.SetEntry(typeof(MiCakeCoreTestModule));
            miCakeApplication.Initialize();

            var resolvedOptions = Services.BuildServiceProvider().GetService<IOptions<MiCakeApplicationOptions>>();

            Assert.NotNull(resolvedOptions);
        }
    }
}
