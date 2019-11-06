using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Tests.DependencyInjection.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.Core.Tests.DependencyInjection
{
    public class DIContainer_Test
    {
        [Fact]
        public void DIContainerAddServiceTest()
        {
            var services = CreateMockServiceCollection();

            DefaultDIContainer dIContainer = new DefaultDIContainer(services);

            dIContainer.AddService<ITestA,ClassA>(MiCakeServiceLifeTime.Singleton);
            var instance = dIContainer.GetService(typeof(ITestA));
            var instanceB = dIContainer.GetService<ITestA>();

            Assert.IsAssignableFrom<ITestA>(instance);
            Assert.Equal(instance.GetType(), instanceB.GetType());
        }


        private IServiceCollection CreateMockServiceCollection()
        {
            var collection = new ServiceCollection();
            return collection;
        }
    }
}
