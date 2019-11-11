using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public abstract class MiCakeApplication : IMiCakeApplication
    {
        public Type StartUpType { get; set; }

        private IMiCakeModuleEngine _miCakeModuleEngine;
        public IMiCakeModuleEngine ModuleEngine => _miCakeModuleEngine;

        private IServiceCollection _services;
        public IServiceCollection Services => _services;

        private IMiCakeApplicationOption _miCakeApplicationOption;
        public IMiCakeApplicationOption MiCakeApplicationOption { get => _miCakeApplicationOption; set => _miCakeApplicationOption = value; }

        private IMiCakeBuilder _miCakeBuilder;

        public MiCakeApplication(Type startUpType, IServiceCollection services, Action<IMiCakeApplicationOption> optionAction = null)
        {
            StartUpType = startUpType;
            _services = services;
            _miCakeApplicationOption = new MiCakeApplicationOption(_services);

            _miCakeBuilder = new MiCakeBuilder(this, optionAction);
            _miCakeBuilder.UseStarpUp(startUpType);
        }

        public virtual void ShutDown(Action<IMiCakeModuleEngine> shutdownAction = null)
        {
            shutdownAction?.Invoke(_miCakeModuleEngine);

            if (StartUpType != null)
                _miCakeModuleEngine.ShutDownModules();
        }

        public void Init()
        {
            _miCakeBuilder.Build();

            _miCakeModuleEngine = _miCakeBuilder.ModuleEngine;
        }
    }
}
