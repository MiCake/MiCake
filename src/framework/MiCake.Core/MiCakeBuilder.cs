using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// Configures MiCake modules and registers services before the DI container is built
    /// </summary>
    internal class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;
        private bool _isBuilt = false;

        /// <summary>
        /// Gets the service collection for registering additional services
        /// </summary>
        public IServiceCollection Services => _services;

        public MiCakeBuilder(
            IServiceCollection services,
            Type entryType,
            MiCakeApplicationOptions options)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _entryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
            _options = options ?? new MiCakeApplicationOptions();

            AddEnvironment();
        }

        /// <summary>
        /// Builds the MiCake configuration and registers all module services.
        /// This must be called to complete the setup and allow modules to register their services.
        /// After calling Build(), no more configuration changes are allowed.
        /// </summary>
        public IMiCakeBuilder Build()
        {
            if (_isBuilt)
                throw new InvalidOperationException(
                    "Build() can only be called once. The builder has already been built.");

            // Validate configuration before building
            ValidateConfiguration();

            // Initialize MiCake and configure all modules synchronously
            // This happens BEFORE the service provider is built
            InitializeMiCakeModules();

            // Register the application factory - it will be resolved after ServiceProvider is built
            RegisterMiCakeApplicationFactory();

            _isBuilt = true;

            return this;
        }

        private void AddEnvironment()
        {
            var environment = new MiCakeEnvironment { EntryType = _entryType };
            _services.AddSingleton<IMiCakeEnvironment>(environment);
        }

        private void ValidateConfiguration()
        {
            if (_entryType == null)
                throw new InvalidOperationException("Entry module type is required.");

            if (!MiCakeModuleHelper.IsMiCakeModule(_entryType))
                throw new InvalidOperationException(
                    $"Entry type '{_entryType.Name}' must inherit from MiCakeModule.");
        }

        private void InitializeMiCakeModules()
        {
            // Create a temporary module manager to discover modules
            var moduleManager = new MiCakeModuleManager();

            // Discover all modules starting from entry module
            moduleManager.PopulateModules(_entryType);

            // Register the module context as singleton
            _services.AddSingleton(moduleManager.ModuleContext);

            // Store options for later
            _services.Configure<MiCakeApplicationOptions>(op => op.Apply(_options));

            // Execute ConfigureServices lifecycle for all modules
            // This allows modules to register their services
            // Reuse the dependency resolver from the module manager to avoid recreating it
            ConfigureModuleServices(moduleManager.ModuleContext, moduleManager.DependencyResolver
                ?? throw new InvalidOperationException("Module dependency resolver is not available. Please check the module initialization process."));
        }

        private void ConfigureModuleServices(IMiCakeModuleContext moduleContext, ModuleDependencyResolver dependencyResolver)
        {
            // Get a temporary logger factory for initialization
            var tempServiceProvider = _services.BuildServiceProvider();
            var loggerFactory = tempServiceProvider.GetService<ILoggerFactory>()
                                ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

            var moduleBoot = new MiCakeModuleBoot(
                loggerFactory,
                moduleContext.MiCakeModules,
                dependencyResolver,
                _options);

            var configServiceContext = new ModuleConfigServiceContext(
                _services,
                moduleContext.MiCakeModules,
                _options);

            moduleBoot.ConfigServices(configServiceContext);

            // Dispose temp service provider
            (tempServiceProvider as IDisposable)?.Dispose();
        }

        private void RegisterMiCakeApplicationFactory()
        {
            // Register a factory that creates MiCakeApplication when needed
            // At this point, all services from modules have been registered
            _services.AddSingleton<IMiCakeApplication>(sp =>
            {
                var moduleContext = sp.GetRequiredService<IMiCakeModuleContext>();
                var options = sp.GetRequiredService<IOptions<MiCakeApplicationOptions>>();

                // Create application with entry type in constructor
                var app = new MiCakeApplication(sp, moduleContext, options, _entryType);

                return app;
            });
        }

        public MiCakeApplicationOptions GetApplicationOptions()
        {
            return _options;
        }
    }
}
