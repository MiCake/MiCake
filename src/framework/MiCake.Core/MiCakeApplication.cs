using MiCake.Core.Data;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace MiCake.Core
{
    /// <summary>
    /// Represents a MiCake application instance.
    /// Manages the module system and application lifecycle.
    /// </summary>
    public class MiCakeApplication : IMiCakeApplication
    {
        private readonly IServiceCollection _services;
        private readonly IServiceScope _appServiceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _needNewScope;
        
        private Type _entryType;
        private IMiCakeModuleBoot _miCakeModuleBoot;
        
        private bool _isInitialized;
        private bool _isStarted;
        private bool _isShutdown;

        /// <summary>
        /// Application options
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        /// <summary>
        /// Module manager
        /// </summary>
        public IMiCakeModuleManager ModuleManager { get; private set; }

        /// <summary>
        /// Service provider for the application
        /// </summary>
        public IServiceProvider AppServiceProvider
        {
            get
            {
                if (_needNewScope && _appServiceScope != null)
                {
                    return _appServiceScope.ServiceProvider;
                }
                return _serviceProvider;
            }
        }

        private IMiCakeModuleContext ModuleContext => ModuleManager?.ModuleContext;

        /// <summary>
        /// Creates a new MiCake application instance.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="serviceProvider">Service provider (required for application startup)</param>
        /// <param name="options">Application options</param>
        /// <param name="needNewScope">Whether to create a new scope</param>
        public MiCakeApplication(
            IServiceCollection services,
            IServiceProvider serviceProvider,
            MiCakeApplicationOptions options,
            bool needNewScope = false)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ApplicationOptions = options ?? new MiCakeApplicationOptions();
            _needNewScope = needNewScope;
            
            ModuleManager = new MiCakeModuleManager();

            if (_needNewScope)
            {
                _appServiceScope = _serviceProvider.CreateScope();
            }
        }

        /// <summary>
        /// Start MiCake application.
        /// </summary>
        public virtual async Task Start()
        {
            if (_isStarted)
                throw new InvalidOperationException("MiCake has already started.");

            if (!_isInitialized)
                throw new InvalidOperationException("MiCake must be initialized before starting. Call Initialize() first.");

            if (AppServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider is not available.");

            _logger?.LogInformation("Starting MiCake Application...");

            // Module Inspection
            var inspectContext = new ModuleInspectionContext(AppServiceProvider, ModuleContext.MiCakeModules);
            foreach (var module in ModuleContext.MiCakeModules)
            {
                if (module is IModuleSelfInspection selfInspection)
                    await selfInspection.Inspect(inspectContext)
                        .ConfigureAwait(false);
            }

            var context = new ModuleLoadContext(AppServiceProvider, ModuleContext.MiCakeModules, ApplicationOptions);
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

            var context = new ModuleLoadContext(AppServiceProvider, ModuleContext.MiCakeModules, ApplicationOptions);
            await _miCakeModuleBoot.ShutDown(context)
                .ConfigureAwait(false);

            _appServiceScope?.Dispose();

            _isShutdown = true;
            _logger?.LogInformation("MiCake Application Shutdown Completed.");
        }

        /// <summary>
        /// Initialize MiCake services
        /// </summary>
        public async Task Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException(
                    $"MiCake has already been initialized. The {nameof(Initialize)} method can only be called once!");

            if (_entryType == null)
                throw new InvalidOperationException(
                    $"Cannot find entry module type. Please make sure you have called {nameof(SetEntry)} method.");

            _logger?.LogInformation("Initializing MiCake Application...");

            AddMiCakeCoreServices(_services);

            // Find all MiCake modules according to the entry module type
            await ModuleManager.PopulateModules(_entryType)
                .ConfigureAwait(false);
            
            _services.AddSingleton(ModuleContext);

            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>() 
                                ?? NullLoggerFactory.Instance;

            _miCakeModuleBoot = new MiCakeModuleBoot(loggerFactory, ModuleContext.MiCakeModules);
            
            // Auto register services to DI
            await _miCakeModuleBoot.AddConfigService(AutoRegisterServices)
                .ConfigureAwait(false);

            var configServiceContext = new ModuleConfigServiceContext(
                _services,
                ModuleContext.MiCakeModules,
                ApplicationOptions);

            await _miCakeModuleBoot.ConfigServices(configServiceContext)
                .ConfigureAwait(false);

            _isInitialized = true;
            _logger?.LogInformation("MiCake Application Initialized Successfully.");
        }

        /// <summary>
        /// Set entry module type.
        /// </summary>
        public void SetEntry(Type type)
        {
            _entryType = type ?? throw new ArgumentException(
                $"Entry module type cannot be null. Please provide a valid module type when calling AddMiCake().");
        }

        private void AddMiCakeCoreServices(IServiceCollection services)
        {
            services.AddSingleton<IMiCakeApplication>(this);
            services.Configure<MiCakeApplicationOptions>(op => op.Apply(ApplicationOptions));
        }

        // Inject service into container according to matching rules
        private void AutoRegisterServices(ModuleConfigServiceContext context)
        {
            var serviceRegistrar = new DefaultServiceRegistrar(context.Services);
            if (ApplicationOptions.FindAutoServiceTypes != null)
                serviceRegistrar.SetServiceTypesFinder(ApplicationOptions.FindAutoServiceTypes);

            serviceRegistrar.Register(context.MiCakeModules);
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
}
