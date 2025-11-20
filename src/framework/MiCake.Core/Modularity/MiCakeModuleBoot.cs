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
        private readonly ModuleDependencyResolver? _dependencyResolver;
        private readonly MiCakeApplicationOptions _applicationOptions;

        private Action<ModuleConfigServiceContext>? _configServiceActions;
        private Action<ModuleInitializationContext>? _initializationActions;

        public MiCakeModuleBoot(
            ILoggerFactory loggerFactory,
            IMiCakeModuleCollection modules,
            ModuleDependencyResolver? dependencyResolver = null,
            MiCakeApplicationOptions? applicationOptions = null)
        {
            _logger = loggerFactory.CreateLogger("MiCake.Core.Modularity.MiCakeModuleBoot");
            _moduleLogger = new MiCakeModuleLogger(_logger);
            _modules = modules;
            _dependencyResolver = dependencyResolver;
            _applicationOptions = applicationOptions ?? new MiCakeApplicationOptions();
        }

        public void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services ??
                throw new ArgumentNullException(nameof(context.Services));

            // Print welcome brand and dependency graph on first lifecycle call
            if (_dependencyResolver != null && _applicationOptions != null)
            {
                _moduleLogger.LogWelcomeAndDependencyGraph(_modules, _dependencyResolver, _applicationOptions);
            }

            _logger.LogInformation("MiCake: Configuring Services......");

            // Pre-configure services (only for advanced modules)
            var preConfigModules = new System.Collections.Generic.List<MiCakeModuleDescriptor>();
            foreach (var module in _modules)
            {
                preConfigModules.Add(module);
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreConfigureServices(context);
                }
            }
            _moduleLogger.LogModuleLifecycle(preConfigModules, "PreConfigureServices");

            // Configure services (standard lifecycle method)
            _moduleLogger.LogModuleLifecycle(_modules, "ConfigureServices");
            foreach (var module in _modules)
            {
                module.Instance.ConfigureServices(context);
            }

            // Post-configure services (only for advanced modules)
            var postConfigModules = new System.Collections.Generic.List<MiCakeModuleDescriptor>();
            foreach (var module in _modules)
            {
                postConfigModules.Add(module);
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PostConfigureServices(context);
                }
            }
            _moduleLogger.LogModuleLifecycle(postConfigModules, "PostConfigureServices");

            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake: Service Configuration Completed.");
        }

        public void Initialization(ModuleInitializationContext context)
        {
            _logger.LogInformation("MiCake: Initializing Application......");

            // Pre-initialization (only for advanced modules)
            var preInitModules = new System.Collections.Generic.List<MiCakeModuleDescriptor>();
            foreach (var module in _modules)
            {
                preInitModules.Add(module);
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreInitialization(context);
                }
            }
            _moduleLogger.LogModuleLifecycle(preInitModules, "PreInitialization");

            // Application initialization (standard lifecycle method)
            _moduleLogger.LogModuleLifecycle(_modules, "OnApplicationInitialization");
            foreach (var module in _modules)
            {
                module.Instance.OnApplicationInitialization(context);
            }

            // Post-initialization (only for advanced modules)
            var postInitModules = new System.Collections.Generic.List<MiCakeModuleDescriptor>();
            foreach (var module in _modules)
            {
                postInitModules.Add(module);
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PostInitialization(context);
                }
            }
            _moduleLogger.LogModuleLifecycle(postInitModules, "PostInitialization");

            _initializationActions?.Invoke(context);

            _logger.LogInformation("MiCake: Application Initialization Completed.");
        }

        public void ShutDown(ModuleShutdownContext context)
        {
            _logger.LogInformation("MiCake: Shutting Down Application......");

            // Pre-shutdown (only for advanced modules)
            var preShutdownModules = new System.Collections.Generic.List<MiCakeModuleDescriptor>();
            foreach (var module in _modules)
            {
                preShutdownModules.Add(module);
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    advancedModule.PreShutdown(context);
                }
            }
            _moduleLogger.LogModuleLifecycle(preShutdownModules, "PreShutdown");

            // Application shutdown (standard lifecycle method)
            _moduleLogger.LogModuleLifecycle(_modules, "OnApplicationShutdown");
            foreach (var module in _modules)
            {
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
