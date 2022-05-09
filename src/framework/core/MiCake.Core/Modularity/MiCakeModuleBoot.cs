using Microsoft.Extensions.Logging;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    internal class MiCakeModuleBoot : IMiCakeModuleBoot
    {
        private readonly ILogger _logger;
        private readonly MiCakeModuleLogger _moduleLogger;
        private readonly IMiCakeModuleCollection _modules;

        private Action<ModuleConfigServiceContext>? _configServiceActions;
        private Action<ModuleLoadContext>? _initializationActions;

        public MiCakeModuleBoot(
            ILoggerFactory loggerFactory,
            IMiCakeModuleCollection modules)
        {
            _logger = loggerFactory.CreateLogger<MiCakeModuleBoot>();
            _moduleLogger = new MiCakeModuleLogger(_logger);
            _modules = modules;
        }

        public void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services ?? throw new ArgumentNullException(nameof(context.Services));

            _logger.LogInformation("MiCake:ActivateServices......");

            StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.ConfigService), context);
            _configServiceActions?.Invoke(context);

            _logger.LogInformation("MiCake:ActivateServices Completed.");
        }

        public void Initialization(ModuleLoadContext context)
        {
            _logger.LogInformation("Initialization MiCake Application......");

            StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Init), context);
            _initializationActions?.Invoke(context);

            _logger.LogInformation("Initialization MiCake Application Completed.");
        }

        public void ShutDown(ModuleLoadContext context)
        {
            _logger.LogInformation("ShutDown MiCake Application......");

            StartModuleLifetime(_modules, bootInfos.Where(s => s.Type == ModuleBootType.Shutdown), context);

            _logger.LogInformation("ShutDown MiCake Application Completed.");
        }

        public void AddConfigService(Action<ModuleConfigServiceContext> configServiceAction)
        {
            _configServiceActions += configServiceAction;
        }

        public void AddInitalzation(Action<ModuleLoadContext> initalzationAction)
        {
            _initializationActions += initalzationAction;
        }

        private void StartModuleLifetime(
            IMiCakeModuleCollection modules,
            IEnumerable<ModuleBootInfo> lifetimes,
            object contextInfo)
        {
            foreach (var lifetimeInfo in lifetimes)
            {
                foreach (var module in modules)
                {
                    //execute current module lifetime.
                    lifetimeInfo.StartAction?.Invoke(module.Instance, contextInfo);
                }

                _moduleLogger.LogModuleInfo(lifetimeInfo.Description, modules);
            }
        }

        #region Lifetimes
        private readonly List<ModuleBootInfo> bootInfos = new()
        {
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PreConfigServices",
                StartAction = (s, context) => s.PreConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "ConfigServices",
                StartAction = (s, context) => s.ConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.ConfigService,
                Description = "PostConfigServices",
                StartAction = (s, context) => s.PostConfigServices((ModuleConfigServiceContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PreInitialization",
                StartAction = (s, context) => s.PreInitialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "Initialization",
                StartAction = (s, context) => s.Initialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Init,
                Description = "PostInitialization",
                StartAction = (s, context) => s.PostInitialization((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "PreShutDown",
                StartAction = (s, context) => s.PreShutDown((ModuleLoadContext)context)
            },
            new ModuleBootInfo()
            {
                Type = ModuleBootType.Shutdown,
                Description = "Shutdown",
                StartAction = (s, context) => s.Shutdown((ModuleLoadContext)context)
            },
        };

        class ModuleBootInfo
        {
            public ModuleBootType Type { get; set; }

            public Action<IMiCakeModule, object>? StartAction { get; set; }

            public string Description { get; set; } = string.Empty;
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
