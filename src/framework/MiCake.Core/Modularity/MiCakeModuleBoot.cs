using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task ConfigServices(ModuleConfigServiceContext context)
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
                    await advancedModule.PreConfigureServices(context)
                        .ConfigureAwait(false);
                }
            }

            // Configure services (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake ConfigureServices: ");
                await module.Instance.ConfigureServices(context)
                    .ConfigureAwait(false);
            }

            // Post-configure services (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PostConfigureServices: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    await advancedModule.PostConfigureServices(context)
                        .ConfigureAwait(false);
                }
            }

            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake: Service Configuration Completed.");
        }

        public async Task Initialization(ModuleInitializationContext context)
        {
            _logger.LogInformation("MiCake: Initializing Application......");

            // Pre-initialization (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PreInitialization: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    await advancedModule.PreInitialization(context)
                        .ConfigureAwait(false);
                }
            }

            // Application initialization (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake OnApplicationInitialization: ");
                await module.Instance.OnApplicationInitialization(context)
                    .ConfigureAwait(false);
            }

            // Post-initialization (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PostInitialization: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    await advancedModule.PostInitialization(context)
                        .ConfigureAwait(false);
                }
            }

            _initializationActions?.Invoke(context);

            _logger.LogInformation("MiCake: Application Initialization Completed.");
        }

        public async Task ShutDown(ModuleShutdownContext context)
        {
            _logger.LogInformation("MiCake: Shutting Down Application......");

            // Pre-shutdown (only for advanced modules)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake PreShutdown: ");
                if (module.Instance is IMiCakeModuleAdvanced advancedModule)
                {
                    await advancedModule.PreShutdown(context)
                        .ConfigureAwait(false);
                }
            }

            // Application shutdown (standard lifecycle method)
            foreach (var module in _modules)
            {
                _moduleLogger.LogModuleInfo(module, "MiCake OnApplicationShutdown: ");
                await module.Instance.OnApplicationShutdown(context)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation("MiCake: Application Shutdown Completed.");
        }

        public Task AddConfigService(Action<ModuleConfigServiceContext> configServiceAction)
        {
            _configServiceActions += configServiceAction;
            return Task.CompletedTask;
        }

        public Task AddInitalzation(Action<ModuleInitializationContext> initalzationAction)
        {
            _initializationActions += initalzationAction;
            return Task.CompletedTask;
        }
    }
}
