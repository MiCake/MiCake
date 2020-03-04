using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    public class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private ILogger<MiCakeModuleBoot> _logger;

        private IMiCakeModuleCollection _modules;
        private MiCakeModuleLogger _moduleLogger;

        private Action<ModuleConfigServiceContext> _configServiceActions;
        private Action<ModuleBearingContext> _initializationActions;

        public MiCakeModuleBoot(
            [NotNull]ILogger<MiCakeModuleBoot> logger,
            [NotNull]IMiCakeModuleCollection modules)
        {
            _logger = logger;
            _moduleLogger = new MiCakeModuleLogger(_logger);
            _modules = modules;
        }

        public void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            if (services == null)
                throw new ArgumentNullException(nameof(services));

            _logger.LogInformation("MiCake:ActivateServices...");

            for (int index = 0; index < configServicesLifetimes.Count; index++)
            {
                var des = configServiceDes[index];
                foreach (var miCakeModule in _modules)
                {
                    _moduleLogger.LogModuleInfo(miCakeModule, $"MiCake {des}: ");
                    configServicesLifetimes[index](miCakeModule.ModuleInstance, context);
                }
            }
            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake:ActivateServices Completed.....");
        }

        public void Initialization(ModuleBearingContext context)
        {
            _logger.LogInformation("Initialization MiCake Application...");

            for (int index = 0; index < initializationLifetimes.Count; index++)
            {
                var des = initializationDes[index];
                foreach (var miCakeModule in _modules)
                {
                    _moduleLogger.LogModuleInfo(miCakeModule, $"MiCake {des}: ");
                    initializationLifetimes[index](miCakeModule.ModuleInstance, context);
                }
            }
            _initializationActions?.Invoke(context);

            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public void ShutDown(ModuleBearingContext context)
        {
            _logger.LogInformation("ShutDown MiCake Application...");

            for (int index = 0; index < shutdownLifetimes.Count; index++)
            {
                var des = shutdownDes[index];
                foreach (var miCakeModule in _modules)
                {
                    _moduleLogger.LogModuleInfo(miCakeModule, $"MiCake {des}: ");
                    shutdownLifetimes[index](miCakeModule.ModuleInstance, context);
                }
            }

            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }

        public void AddConfigService([NotNull]Action<ModuleConfigServiceContext> configServiceAction)
        {
            _configServiceActions += configServiceAction;
        }

        public void AddInitalzation([NotNull]Action<ModuleBearingContext> initalzationAction)
        {
            _initializationActions += initalzationAction;
        }

        #region LifeTimes
        private string[] configServiceDes = { "PreConfigServices", "ConfigServices", "PostConfigServices" };
        private List<Action<IModuleConfigServicesLifeTime, ModuleConfigServiceContext>> configServicesLifetimes =
            new List<Action<IModuleConfigServicesLifeTime, ModuleConfigServiceContext>>
        {
            (s,context) => s.PreConfigServices(context),
             (s,context) => s.ConfigServices(context),
              (s,context) => s.PostConfigServices(context)
        };

        private string[] initializationDes = { "PreInitialization", "Initialization", "PostInitialization" };
        private List<Action<IModuleLifeTime, ModuleBearingContext>> initializationLifetimes =
            new List<Action<IModuleLifeTime, ModuleBearingContext>>
        {
            (s,context) => s.PreInitialization(context),
             (s,context) => s.Initialization(context),
              (s,context) => s.PostInitialization(context)
        };

        private string[] shutdownDes = { "PreShutDown", "Shutdown" };
        private List<Action<IModuleLifeTime, ModuleBearingContext>> shutdownLifetimes =
            new List<Action<IModuleLifeTime, ModuleBearingContext>>
        {
            (s,context) => s.PreShutDown(context),
             (s,context) => s.Shutdown(context),
        };
        #endregion
    }
}
