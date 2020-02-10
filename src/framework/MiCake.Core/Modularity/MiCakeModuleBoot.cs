using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.Logging;
using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    public class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private ILogger<MiCakeModuleBoot> _logger;
        private IMiCakeBuilder _miCakeBuilder;

        public MiCakeModuleBoot(
            ILogger<MiCakeModuleBoot> logger,
            IMiCakeBuilder miCakeBuilder)
        {
            _logger = logger;
            _miCakeBuilder = miCakeBuilder;
        }

        public void Initialization(ModuleBearingContext context)
        {
            if (_miCakeBuilder == null)
                throw new ArgumentException(nameof(_miCakeBuilder));

            _logger.LogInformation("Initialization MiCake Application...");
            var moduleLogger = new MiCakeModuleLogger(_logger);

            var modules = MiCakeModuleHelper.CombineNormalAndFeatureModules(
                                                _miCakeBuilder.ModuleManager.MiCakeModules,
                                                _miCakeBuilder.ModuleManager.FeatureModules);

            //preInit
            foreach (var module in modules)
            {
                moduleLogger.LogModuleInfo(module, "MiCake PreModuleInitialization: ");
                module.ModuleInstance.PreModuleInitialization(context);
            }
            //Init
            foreach (var module in modules)
            {
                moduleLogger.LogModuleInfo(module, "MiCake Initialization: ");
                module.ModuleInstance.Initialization(context);
            }
            //PostInit
            foreach (var module in modules)
            {
                moduleLogger.LogModuleInfo(module, "MiCake PostModuleInitialization: ");
                module.ModuleInstance.PostModuleInitialization(context);
            }

            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public void ShutDown(ModuleBearingContext context)
        {
            if (_miCakeBuilder == null)
                throw new ArgumentException(nameof(_miCakeBuilder));

            _logger.LogInformation("ShutDown MiCake Application...");
            var moduleLogger = new MiCakeModuleLogger(_logger);

            var modules = MiCakeModuleHelper.CombineNormalAndFeatureModules(
                                                _miCakeBuilder.ModuleManager.MiCakeModules,
                                                _miCakeBuilder.ModuleManager.FeatureModules);

            //PreModuleShutDown
            foreach (var module in modules)
            {
                moduleLogger.LogModuleInfo(module, "MiCake PreModuleShutDown: ");
                module.ModuleInstance.PreModuleShutDown(context);
            }
            //Shuntdown
            foreach (var module in modules)
            {
                moduleLogger.LogModuleInfo(module, "MiCake Shuntdown: ");
                module.ModuleInstance.Shuntdown(context);
            }

            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }
    }
}
