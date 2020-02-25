using MiCake.Core.Builder;
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

        private IMiCakeModuleCollection Modules;
        private MiCakeModuleLogger ModuleLogger;

        public MiCakeModuleBoot(
            ILogger<MiCakeModuleBoot> logger,
            IMiCakeBuilder miCakeBuilder)
        {
            if (miCakeBuilder == null)
                throw new ArgumentException(nameof(IMiCakeBuilder));

            _logger = logger;
            ModuleLogger = new MiCakeModuleLogger(_logger);
            Modules = miCakeBuilder.ModuleManager.AllModules;
        }

        public void ConfigServices(
            ModuleConfigServiceContext context,
            Action<ModuleConfigServiceContext> otherPartConfigServicesAction = null)
        {
            var services = context.Services;

            if (services == null)
                throw new ArgumentNullException(nameof(services));

            _logger.LogInformation("MiCake:ActivateServices...");

            //PreConfigServices
            foreach (var miCakeModule in Modules)
            {
                ModuleLogger.LogModuleInfo(miCakeModule, "MiCake PreConfigServices: ");
                miCakeModule.ModuleInstance.PreConfigServices(context);
            }
            //ConfigServices
            foreach (var miCakeModule in Modules)
            {
                ModuleLogger.LogModuleInfo(miCakeModule, "MiCake ConfigServiices: ");
                miCakeModule.ModuleInstance.ConfigServices(context);
            }
            //PostConfigServices
            foreach (var miCakeModule in Modules)
            {
                ModuleLogger.LogModuleInfo(miCakeModule, "MiCake PostConfigServices: ");
                miCakeModule.ModuleInstance.PostConfigServices(context);
            }

            //Activate Other Part Services 
            otherPartConfigServicesAction?.Invoke(context);

            _logger.LogInformation("MiCake:ActivateServices Completed.....");
        }

        public void Initialization(
            ModuleBearingContext context,
            Action<ModuleBearingContext> otherPartInitAction = null)
        {
            _logger.LogInformation("Initialization MiCake Application...");

            //preInit
            foreach (var module in Modules)
            {
                ModuleLogger.LogModuleInfo(module, "MiCake PreModuleInitialization: ");
                module.ModuleInstance.PreModuleInitialization(context);
            }
            //Init
            foreach (var module in Modules)
            {
                ModuleLogger.LogModuleInfo(module, "MiCake Initialization: ");
                module.ModuleInstance.Initialization(context);
            }
            //PostInit
            foreach (var module in Modules)
            {
                ModuleLogger.LogModuleInfo(module, "MiCake PostModuleInitialization: ");
                module.ModuleInstance.PostModuleInitialization(context);
            }

            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public void ShutDown(ModuleBearingContext context)
        {
            _logger.LogInformation("ShutDown MiCake Application...");

            //PreModuleShutDown
            foreach (var module in Modules)
            {
                ModuleLogger.LogModuleInfo(module, "MiCake PreModuleShutDown: ");
                module.ModuleInstance.PreModuleShutDown(context);
            }
            //Shuntdown
            foreach (var module in Modules)
            {
                ModuleLogger.LogModuleInfo(module, "MiCake Shuntdown: ");
                module.ModuleInstance.Shuntdown(context);
            }

            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }
    }
}
