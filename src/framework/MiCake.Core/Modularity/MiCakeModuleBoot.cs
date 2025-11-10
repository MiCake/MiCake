using Microsoft.Extensions.Logging;
using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module boot - Used to initialize modules and shutdown modules.
    /// Handles the complete lifecycle of modules based on the simplified 3-method approach
    /// with support for advanced modules that need fine-grained control.
    /// </summary>
    internal class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private readonly ILogger _logger;
        private readonly IMiCakeModuleCollection _modules;
        private readonly MiCakeModuleLogger _moduleLogger;

        private Action<ModuleConfigServiceContext> _configServiceActions;
        private Action<ModuleInitializationContext> _initializationActions;

        public MiCakeModuleBoot(
            ILoggerFactory loggerFactory,
            IMiCakeModuleCollection modules)
        {
            _logger = loggerFactory.CreateLogger("MiCake.Core.Modularity.MiCakeModuleBoot");
            _moduleLogger = new MiCakeModuleLogger(_logger);
            _modules = modules;
        }

        public void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services ??
                throw new ArgumentNullException(nameof(context.Services));

            _logger.LogInformation("MiCake: Configuring Services......");

            // Pre-configure services (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PreConfigureServices: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreConfigureServices(context);
                }
            }

            // Configure services (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake ConfigureServices: ");
                module.Instance.ConfigureServices(context);
            }

            // Post-configure services (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PostConfigureServices: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PostConfigureServices(context);
                }
            }

            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake: Service Configuration Completed.");
        }

        public void Initialization(ModuleInitializationContext context)
        {
            _logger.LogInformation("MiCake: Initializing Application......");

            // Pre-initialization (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PreInitialization: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreInitialization(context);
                }
            }

            // Application initialization (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake OnApplicationInitialization: ");
                module.Instance.OnApplicationInitialization(context);
            }

            // Post-initialization (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PostInitialization: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PostInitialization(context);
                }
            }

            _initializationActions?.Invoke(context);

            _logger.LogInformation("MiCake: Application Initialization Completed.");
        }

        public void ShutDown(ModuleShutdownContext context)
        {
            _logger.LogInformation("MiCake: Shutting Down Application......");

            // Pre-shutdown (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PreShutdown: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreShutdown(context);
                }
            }

            // Application shutdown (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake OnApplicationShutdown: ");
                module.Instance.OnApplicationShutdown(context);
            }

            _logger.LogInformation("MiCake: Application Shutdown Completed.");
        }

        public void AddConfigService(Action<ModuleConfigServiceContext> configServiceAction)
        {
            _configServiceActions += configServiceAction;
        }

        public void AddInitalzation(Action<ModuleInitializationContext> initalzationAction)
        {
            _initializationActions += initalzationAction;
        }
    }
}
