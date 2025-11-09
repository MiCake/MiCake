using MiCake.Core.Data;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        
        private Type _entryType;
        private IMiCakeModuleBoot _miCakeModuleBoot;
        
        private bool _isStarted;
        private bool _isShutdown;

        /// <summary>
        /// Application options
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        /// <summary>
        /// Module manager - provides access to module context
        /// </summary>
        public IMiCakeModuleManager ModuleManager { get; private set; }

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
        public MiCakeApplication(
            IServiceProvider serviceProvider,
            IMiCakeModuleContext moduleContext,
            MiCakeApplicationOptions options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
            ApplicationOptions = options ?? new MiCakeApplicationOptions();
            
            // Create a simple module manager wrapper for the context
            ModuleManager = new MiCakeModuleManagerWrapper(_moduleContext);
        }

        /// <summary>
        /// Start MiCake application.
        /// Executes the initialization lifecycle for all modules.
        /// </summary>
        public virtual async Task Start()
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
                    await selfInspection.Inspect(inspectContext)
                        .ConfigureAwait(false);
            }

            var context = new ModuleInitializationContext(AppServiceProvider, _moduleContext.MiCakeModules, ApplicationOptions);
            await _miCakeModuleBoot.Initialization(context)
                .ConfigureAwait(false);

            // Release options additional info
            ApplicationOptions.ExtraDataStash.Release();

            _isStarted = true;
            _logger?.LogInformation("MiCake Application Started Successfully.");
        }

        /// <summary>
        /// Shutdown MiCake application
        /// </summary>
        public virtual async Task ShutDown()
        {
            if (_isShutdown)
                throw new InvalidOperationException("MiCake has already shutdown.");

            _logger?.LogInformation("Shutting Down MiCake Application...");

            if (_miCakeModuleBoot != null)
            {
                var context = new ModuleShutdownContext(AppServiceProvider, _moduleContext.MiCakeModules, ApplicationOptions);
                await _miCakeModuleBoot.ShutDown(context)
                    .ConfigureAwait(false);
            }

            _isShutdown = true;
            _logger?.LogInformation("MiCake Application Shutdown Completed.");
        }

        /// <summary>
        /// Initialize MiCake services (deprecated - now handled by MiCakeBuilder)
        /// </summary>
        [Obsolete("Initialization is now handled by MiCakeBuilder. This method is kept for compatibility.")]
        public Task Initialize()
        {
            // No-op - initialization is now done in MiCakeBuilder
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set entry module type.
        /// </summary>
        public void SetEntry(Type type)
        {
            _entryType = type ?? throw new ArgumentException(
                $"Entry module type cannot be null. Please provide a valid module type when calling AddMiCake().");
        }

        public async ValueTask DisposeAsync()
        {
            if (!_isShutdown)
                await ShutDown().ConfigureAwait(false);

            ModuleManager = null;
        }

        private ILogger _logger => _serviceProvider?.GetService<ILoggerFactory>()
            ?.CreateLogger<MiCakeApplication>();
    }
    
    /// <summary>
    /// Simple wrapper for IMiCakeModuleContext to provide IMiCakeModuleManager interface
    /// </summary>
    internal class MiCakeModuleManagerWrapper : IMiCakeModuleManager
    {
        private readonly IMiCakeModuleContext _context;
        
        public MiCakeModuleManagerWrapper(IMiCakeModuleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        
        public Func<Type, object> ServiceCtor { get; private set; }
        public IMiCakeModuleContext ModuleContext => _context;
        public bool IsPopulated => true;
        
        public Task PopulateModules(Type entryType)
        {
            throw new InvalidOperationException("Modules are already populated.");
        }
        
        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return _context?.MiCakeModules.FirstOrDefault(s => s.ModuleType == moduleType);
        }
        
        public Task AddMiCakeModule(Type moduleType)
        {
            throw new InvalidOperationException("Cannot add modules after initialization.");
        }
    }
}
