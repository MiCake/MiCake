using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    internal class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private readonly ILogger _logger;

        private readonly IMiCakeModuleCollection _modules;
        private readonly MiCakeModuleLogger _moduleLogger;

        private Action<ModuleConfigServiceContext> _configServiceActions;
        private Action<ModuleLoadContext> _initializationActions;

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

            _logger.LogInformation("MiCake:ActivateServices......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.ConfigService), context);
            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake:ActivateServices Completed.");
        }

        public async Task Initialization(ModuleLoadContext context)
        {
            _logger.LogInformation("Initialization MiCake Application......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Init), context);
            _initializationActions?.Invoke(context);

            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public async Task ShutDown(ModuleLoadContext context)
        {
            _logger.LogInformation("ShutDown MiCake Application......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Shutdown), context);

            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }

        public Task AddConfigService(Action<ModuleConfigServiceContext> configServiceAction)
        {
            _configServiceActions += configServiceAction;

            return Task.CompletedTask;
        }

        public Task AddInitalzation(Action<ModuleLoadContext> initalzationAction)
        {
            _initializationActions += initalzationAction;

            return Task.CompletedTask;
        }

        private async Task StartModuleLifetime(
            IMiCakeModuleCollection modules,
            IEnumerable<ModuleBootInfo> lifetimes,
            object contextInfo)
        {
            foreach (var lifetimeInfo in lifetimes)
            {
                foreach (var module in modules)
                {
                    //logging current module info.
                    _moduleLogger.LogModuleInfo(module, $"MiCake {lifetimeInfo.Description}: ");
                    //execute current module lifetime.
                    await lifetimeInfo.StartAction(module.Instance, contextInfo);
                }
            }
        }

        #region LifeTimes
        private readonly List<ModuleBootInfo> bootInfos =
        [
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PreConfigServices",
                StartAction = async (s, context) => await s.PreConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "ConfigServices",
                StartAction = async (s, context) => await s.ConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PostConfigServices",
                StartAction = async (s, context) => await s.PostConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PreInitialization",
                StartAction = async (s, context) => await s.PreInitialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "Initialization",
                StartAction = async (s, context) => await s.Initialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PostInitialization",
                StartAction = async (s, context) => await s.PostInitialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "PreShutDown",
                StartAction = async (s, context) => await s.PreShutDown((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "Shutdown",
                StartAction = async (s, context) => await s.Shutdown((ModuleLoadContext)context)
            },
        ];

        class ModuleBootInfo
        {
            public ModuleBootType Type { get; set; }

            public Func<IMiCakeModule, object, Task> StartAction { get; set; }

            public string Description { get; set; }
        }

        enum ModuleBootType
        {
            ConfigService,
            Init,
            Shutdown
        }
        #endregion
    }
}
