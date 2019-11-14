using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Builder
{
    public interface IMiCakeBuilder
    {
        public IMiCakeModuleManager ModuleManager { get; }

        public IServiceCollection Services { get; }
    }
}
