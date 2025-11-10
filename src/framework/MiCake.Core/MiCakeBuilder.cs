using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// A builder for <see cref="IMiCakeApplication"/>
    /// Configures MiCake modules and registers services before the DI container is built
    /// </summary>
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;

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
        /// </summary>
        public IMiCakeBuilder Build()
        {
            // Initialize MiCake and configure all modules synchronously
            // This happens BEFORE the service provider is built
            InitializeMiCakeModules();
            
            // Register the application factory - it will be resolved after ServiceProvider is built
            RegisterMiCakeApplicationFactory();
            
            return this;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp)
        {
            // This can be stored and applied when the application is created
            return this;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp)
        {
            // This can be stored and applied when the application is created
            return this;
        }

        private void AddEnvironment()
        {
            var environment = new MiCakeEnvironment { EntryType = _entryType };
            _services.AddSingleton<IMiCakeEnvironment>(environment);
        }

        private void InitializeMiCakeModules()
        {
            // Create a temporary module manager to discover modules
            var moduleManager = new MiCakeModuleManager();
            
            // Discover all modules starting from entry module
            moduleManager.PopulateModules(_entryType).GetAwaiter().GetResult();
            
            // Register the module context as singleton
            _services.AddSingleton(moduleManager.ModuleContext);
            
            // Store entry type and options for later
            _services.AddSingleton(_options);
            _services.AddSingleton(new MiCakeModuleEntryInfo(_entryType));
            _services.Configure<MiCakeApplicationOptions>(op => op.Apply(_options));
            
            // Execute ConfigureServices lifecycle for all modules
            // This allows modules to register their services
            ConfigureModuleServices(moduleManager.ModuleContext);
        }

        private void ConfigureModuleServices(IMiCakeModuleContext moduleContext)
        {
            // Get a temporary logger factory for initialization
            var tempServiceProvider = _services.BuildServiceProvider();
            var loggerFactory = tempServiceProvider.GetService<ILoggerFactory>() 
                                ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            
            var moduleBoot = new MiCakeModuleBoot(loggerFactory, moduleContext.MiCakeModules);
            
            var configServiceContext = new ModuleConfigServiceContext(
                _services,
                moduleContext.MiCakeModules,
                _options);
            
            // Execute ConfigureServices for all modules (including MiCakeRootModule which handles auto-registration)
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
                var entryInfo = sp.GetRequiredService<MiCakeModuleEntryInfo>();
                var options = sp.GetRequiredService<MiCakeApplicationOptions>();
                
                var app = new MiCakeApplication(sp, moduleContext, options);
                app.SetEntry(entryInfo.EntryType);
                
                return app;
            });
        }
    }
    
    /// <summary>
    /// Internal class to store entry module information
    /// </summary>
    internal class MiCakeModuleEntryInfo
    {
        public Type EntryType { get; }
        
        public MiCakeModuleEntryInfo(Type entryType)
        {
            EntryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
        }
    }
}
