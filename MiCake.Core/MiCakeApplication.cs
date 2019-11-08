using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core
{
    public abstract class MiCakeApplication : IMiCakeApplication
    {
        public Type StartUpType { get; set; }

        private IMiCakeModuleEngine _miCakeModuleEngine;
        public IMiCakeModuleEngine ModuleEngine => _miCakeModuleEngine;

        private IServiceCollection _services;
        public IServiceCollection Services => _services;

        private IMiCakeBuilder _miCakeBuilder;

        public MiCakeApplication(Type startUpType, IServiceCollection services)
        {
            StartUpType = startUpType;
            _services = services;

            _miCakeBuilder = new MiCakeBuilder(_services);
            _miCakeBuilder.UseStarpUp(startUpType);
            _miCakeBuilder.Build();

            _miCakeModuleEngine = _miCakeBuilder.ModuleEngine;
        }

        public virtual void ShutDown(Action<IMiCakeModuleEngine> shutdownAction = null)
        {
            shutdownAction?.Invoke(_miCakeModuleEngine);

            if (StartUpType != null)
                _miCakeModuleEngine.ShutDownModules();
        }
    }
}
