using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// 
    /// MiCake 模块启动器，用于初始化模块和关闭模块
    /// </summary>
    public class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private ILogger<MiCakeModuleBoot> _logger;
        private IServiceProvider _serviceProvider;
        private IMiCakeBuilder _miCakeBuilder;

        public MiCakeModuleBoot(
            ILogger<MiCakeModuleBoot> logger,
            IServiceProvider serviceProvider,
            IMiCakeBuilder miCakeBuilder)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _miCakeBuilder = miCakeBuilder;
        }

        public void Initialization(ModuleBearingContext context)
        {
            if (_serviceProvider == null)
                throw new ArgumentException(nameof(_serviceProvider));

            if (_miCakeBuilder == null)
                throw new ArgumentException(nameof(_miCakeBuilder));

            _logger.LogInformation("Initialization MiCake Application...");

            var modules = _miCakeBuilder.ModuleManager.miCakeModules;

            //preInit
            foreach (var module in modules)
            {
                _logger.LogInformation($"MiCake LiftTime-PreModuleInitialization:{ module.Type.Name }");
                module.ModuleInstance.PreModuleInitialization(context);
            }
            //Init
            foreach (var module in modules)
            {
                _logger.LogInformation($"MiCake LiftTime-Initialization:{ module.Type.Name }");
                module.ModuleInstance.Initialization(context);
            }
            //PostInit
            foreach (var module in modules)
            {
                _logger.LogInformation($"MiCake LiftTime-PostModuleInitialization:{ module.Type.Name }");
                module.ModuleInstance.PostModuleInitialization(context);
            }
            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public void ShutDown(ModuleBearingContext context)
        {
            if (_serviceProvider == null)
                throw new ArgumentException(nameof(_serviceProvider));

            if (_miCakeBuilder == null)
                throw new ArgumentException(nameof(_miCakeBuilder));

            _logger.LogInformation("ShutDown MiCake Application...");

            var modules = _miCakeBuilder.ModuleManager.miCakeModules;

            //PreModuleShutDown
            foreach (var module in modules)
            {
                _logger.LogInformation($"MiCake LiftTime-PreModuleShutDown:{ module.Type.Name }");
                module.ModuleInstance.PreModuleShutDown(context);
            }
            //Shuntdown
            foreach (var module in modules)
            {
                _logger.LogInformation($"MiCake LiftTime-Shuntdown:{ module.Type.Name }");
                module.ModuleInstance.Shuntdown(context);
            }
            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }
    }
}
