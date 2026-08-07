using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// The public ambient accessor must remain the single authoritative view of the
    /// ambient unit of work state written by the unit of work manager. The framework
    /// mapping wins over a host-registered replacement; a split would make interceptors
    /// observe a different frame stack than the manager writes.
    /// </summary>
    public class UnitOfWorkAmbientAccessorRegistrationTests
    {
        private sealed class ReplacementAccessor : IUnitOfWorkAmbientAccessor
        {
            public System.IServiceProvider? CurrentServiceProvider => null;
        }

        [Fact]
        public void ModuleRegistration_MappingWins_OverHostRegisteredReplacement()
        {
            // Arrange - the host registers its own implementation before the module runs
            var services = new ServiceCollection();
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(new ReplacementAccessor());

            var module = new MiCakeEssentialModule();
            var context = new ModuleConfigServiceContext(
                services,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());

            // Act - the framework module registers the authoritative mapping
            module.PreConfigureServices(context);
            using var provider = services.BuildServiceProvider();

            // Assert - the resolved accessor is the framework mapping, not the replacement
            var resolved = provider.GetRequiredService<IUnitOfWorkAmbientAccessor>();
            Assert.Same(provider.GetRequiredService<AmbientUnitOfWorkAccessor>(), resolved);
        }
    }
}
