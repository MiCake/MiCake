using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Tests.DependencyInjection.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.DependencyInjection
{
    public class DIContainer_Test
    {
        [Fact]
        public void DIContainerAddServiceTest()
        {
        }


        private IServiceCollection CreateMockServiceCollection()
        {
            var collection = new ServiceCollection();
            return collection;
        }
    }
}
