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

            _logger.LogInformation("MiCake: Configuring Services......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.ConfigService), context);
            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake: Service Configuration Completed.");
        }

        public async Task Initialization(ModuleLoadContext context)
        {
            _logger.LogInformation("MiCake: Initializing Application......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Init), context);
            _initializationActions?.Invoke(context);

            _logger.LogInformation("MiCake: Application Initialization Completed.");
        }

        public async Task ShutDown(ModuleLoadContext context)
        {
            _logger.LogInformation("MiCake: Shutting Down Application......");

            await StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Shutdown), context);

            _logger.LogInformation("MiCake: Application Shutdown Completed.");
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
                    // Logging current module info
                    _moduleLogger.LogModuleInfo(module, $"MiCake {lifetimeInfo.Description}: ");
                    
                    // Execute current module lifetime
                    await lifetimeInfo.StartAction(module.Instance, contextInfo)
                        .ConfigureAwait(false);
                }
            }
        }

        #region LifeTimes
        private readonly List<ModuleBootInfo> bootInfos =
        [
            // Pre-configure services (only for advanced modules)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PreConfigureServices",
                StartAction = async (s, context) =>
                {
                    if (s is IMiCakeModuleAdvanced advancedModule)
                    {
                        await advancedModule.PreConfigureServices(context)
                            .ConfigureAwait(false);
                    }
                }
            },
            // Configure services (standard lifecycle method)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "ConfigureServices",
                StartAction = async (s, context) =>
                {
                    await s.ConfigureServices(context)
                        .ConfigureAwait(false);
                }
            },
            // Post-configure services (only for advanced modules)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PostConfigureServices",
                StartAction = async (s, context) =>
                {
                    if (s is IMiCakeModuleAdvanced advancedModule)
                    {
                        await advancedModule.PostConfigureServices(context)
                            .ConfigureAwait(false);
                    }
                }
            },
            // Pre-initialization (only for advanced modules)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PreInitialization",
                StartAction = async (s, context) =>
                {
                    if (s is IMiCakeModuleAdvanced advancedModule)
                    {
                        await advancedModule.PreInitialization(context)
                            .ConfigureAwait(false);
                    }
                }
            },
            // Application initialization (standard lifecycle method)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "OnApplicationInitialization",
                StartAction = async (s, context) =>
                {
                    await s.OnApplicationInitialization(context)
                        .ConfigureAwait(false);
                }
            },
            // Post-initialization (only for advanced modules)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PostInitialization",
                StartAction = async (s, context) =>
                {
                    if (s is IMiCakeModuleAdvanced advancedModule)
                    {
                        await advancedModule.PostInitialization(context)
                            .ConfigureAwait(false);
                    }
                }
            },
            // Pre-shutdown (only for advanced modules)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "PreShutdown",
                StartAction = async (s, context) =>
                {
                    if (s is IMiCakeModuleAdvanced advancedModule)
                    {
                        await advancedModule.PreShutdown(context)
                            .ConfigureAwait(false);
                    }
                }
            },
            // Application shutdown (standard lifecycle method)
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "OnApplicationShutdown",
                StartAction = async (s, context) =>
                {
                    await s.OnApplicationShutdown(context)
                        .ConfigureAwait(false);
                }
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
