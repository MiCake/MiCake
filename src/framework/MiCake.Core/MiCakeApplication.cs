using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Represents a MiCake application instance.
    /// Manages the application lifecycle (initialization and shutdown).
    /// Module discovery and service configuration happens earlier in MiCakeBuilder.
    /// </summary>
    public class MiCakeApplication : IMiCakeApplication
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMiCakeModuleContext _moduleContext;
        private readonly Type _entryType;
        
        private IMiCakeModuleBoot _miCakeModuleBoot;
        
        private bool _isStarted;
        private bool _isShutdown;

        /// <summary>
        /// Application options
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        /// <summary>
        /// Module context - provides read-only access to loaded modules
        /// </summary>
        public IMiCakeModuleContext ModuleContext => _moduleContext;

        /// <summary>
        /// Service provider for the application
        /// </summary>
        public IServiceProvider AppServiceProvider => _serviceProvider;

        /// <summary>
        /// Creates a new MiCake application instance.
        /// This constructor is called by the DI container after all services are registered.
        /// </summary>
        /// <param name="serviceProvider">Service provider (injected by DI container)</param>
        /// <param name="moduleContext">Module context with all discovered modules</param>
        /// <param name="options">Application options</param>
        /// <param name="entryType">Entry module type</param>
        public MiCakeApplication(
            IServiceProvider serviceProvider,
            IMiCakeModuleContext moduleContext,
            IOptions<MiCakeApplicationOptions> options,
            Type entryType)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
            _entryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
            ApplicationOptions = options.Value ?? new MiCakeApplicationOptions();
        }

        /// <summary>
        /// Start MiCake application.
        /// Executes the initialization lifecycle for all modules.
        /// </summary>
        public virtual void Start()
        {
            if (_isStarted)
                throw new InvalidOperationException("MiCake has already started.");

            if (AppServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider is not available.");

            _logger?.LogInformation("Starting MiCake Application...");

            // Initialize module boot if not already done
            if (_miCakeModuleBoot == null)
            {
                var loggerFactory = _serviceProvider.GetService<ILoggerFactory>() 
                                    ?? NullLoggerFactory.Instance;
                _miCakeModuleBoot = new MiCakeModuleBoot(loggerFactory, _moduleContext.MiCakeModules);
            }

            // Module Inspection
            var inspectContext = new ModuleInspectionContext(AppServiceProvider, _moduleContext.MiCakeModules);
            foreach (var module in _moduleContext.MiCakeModules)
            {
                if (module is IModuleSelfInspection selfInspection)
                {
                    selfInspection.Inspect(inspectContext);
                }
            }

            var context = new ModuleInitializationContext(AppServiceProvider, _moduleContext.MiCakeModules, ApplicationOptions);
            _miCakeModuleBoot.Initialization(context);

            // Release options additional info
            ApplicationOptions.BuildTimeData.Release();

            _isStarted = true;
            _logger?.LogInformation("MiCake Application Started Successfully.");
        }

        /// <summary>
        /// Shutdown MiCake application
        /// </summary>
        public virtual void ShutDown()
        {
            if (_isShutdown)
                throw new InvalidOperationException("MiCake has already shutdown.");

            _logger?.LogInformation("Shutting Down MiCake Application...");

            if (_miCakeModuleBoot != null)
            {
                var context = new ModuleShutdownContext(AppServiceProvider, _moduleContext.MiCakeModules, ApplicationOptions);
                _miCakeModuleBoot.ShutDown(context);
            }

            _isShutdown = true;
            _logger?.LogInformation("MiCake Application Shutdown Completed.");
        }

        public void Dispose()
        {
            if (!_isShutdown)
                ShutDown();
        }

        private ILogger _logger => _serviceProvider?.GetService<ILoggerFactory>()
            ?.CreateLogger<MiCakeApplication>();
    }
}
