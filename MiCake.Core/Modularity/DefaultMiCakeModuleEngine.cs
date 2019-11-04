using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MiCake.Core.Modularity
{
    public class DefaultMiCakeModuleEngine : MiCakeModuleEngine
    {
        public DefaultMiCakeModuleEngine(IServiceCollection services, ILogger<MiCakeModuleEngine> logger) : base(services, logger)
        {
        }
    }
}
