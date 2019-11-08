using MiCake.Core.Abstractions.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiCake.Core.Modularity;
using System;
using Microsoft.Extensions.Logging.Abstractions;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Abstractions.Log;
using MiCake.Core.Log;
using MiCake.Core.ExceptionHandling;
using MiCake.Core.Abstractions.ExceptionHandling;

namespace MiCake.Core.Builder
{
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private bool _hasBuild;
        private Type _startUp;
        private IServiceCollection _services;

        private IMiCakeModuleEngine _moduleEngine;
        public IMiCakeModuleEngine ModuleEngine => _moduleEngine;

        public MiCakeBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void Build()
        {
            if (_hasBuild)
                throw new InvalidOperationException("Build can only be called once.");

            _hasBuild = true;

            if (_startUp == null) return;

            ConfigurePreServices();

            _moduleEngine = ConfigMiCakeModuleEngine();
            _moduleEngine.InitializeModules();
        }

        private void ConfigurePreServices()
        {
            //add default log error handler
            _services.AddSingleton<ILogErrorHandlerProvider, DefaultLogErrorHandlerProvider>();
            _services.AddSingleton<IMiCakeErrorHandler, DefaultMiCakeErrorHandler>();
        }

        private IMiCakeModuleEngine ConfigMiCakeModuleEngine()
        {
            var engineLogger = _services.BuildServiceProvider().GetService<ILogger<MiCakeModuleEngine>>() ?? NullLogger<MiCakeModuleEngine>.Instance;

            IMiCakeModuleEngine miCakeModuleEngine = new DefaultMiCakeModuleEngine(_services, engineLogger);
            miCakeModuleEngine.LoadMiCakeModules(_startUp);

            //add di container
            _services.AddTransient<IDIContainer, DefaultDIContainer>();
            //configure di manager to module
            miCakeModuleEngine.ConfigureModule(s =>
            {
                var defaultDIManager = new DefaultMiCakeDIManager(_services);
                defaultDIManager.PopulateAutoService(s);
            });

            return miCakeModuleEngine;
        }

        public IMiCakeBuilder UseStarpUp(Type startUp)
        {
            _startUp = startUp;
            return this;
        }
    }
}
