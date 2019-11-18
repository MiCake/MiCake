using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    public class ModuleCollection_Test
    {
        [Fact]
        public void MiCakeModuleManagerPopulateDefaultModule_Test()
        {
            var moduleManager = new MiCakeModuleManager();
         //   moduleManager.PopulateDefaultModule(typeof(StarpUpModule));
        }
    }
}
