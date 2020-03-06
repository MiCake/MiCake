using JetBrains.Annotations;
using MiCake.Core.Data;
using MiCake.Core.DependencyInjection;
using MiCake.Core.ExceptionHandling;
using MiCake.Core.Logging;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace MiCake.Core
{
    public class MiCakeApplication : IMiCakeApplication, INeedNecessaryParts<IServiceProvider>
    {
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }
        public IMiCakeModuleManager ModuleManager { get; private set; } = new MiCakeModuleManager();

        private readonly IServiceCollection _services;
        private IServiceScope _appServiceScope;

        private IServiceProvider _appServiceProvider;
        public IServiceProvider AppServiceProvider
        {
            get
            {
                if (_appServiceProvider == null)
                {
                    if (_needNewScope)
                        _appServiceScope = _serviceProvider.CreateScope();

                    _appServiceProvider = _appServiceScope == null ?
                                                    _serviceProvider :
                                                    _appServiceScope.ServiceProvider;
                }
                return _appServiceProvider;
            }
        }

        private IMiCakeModuleContext ModuleContext => ModuleManager?.ModuleContext;

        private Type _entryType;
        private IMiCakeModuleBoot _miCakeModuleBoot;
        private IServiceProvider _serviceProvider;
        private bool _needNewScope;

        private bool _isInitialized;
        private bool _isStarted;
        private bool _isShutdown;

        public MiCakeApplication(
            [NotNull]IServiceCollection services,
            [NotNull]MiCakeApplicationOptions options,
            bool needNewScope)
        {
            ApplicationOptions = options;
            _services = services;
            _needNewScope = needNewScope;
        }

        /// <summary>
        /// Start micake application.
        /// Must provider <see cref="IServiceProvider"/>
        /// </summary>
        public virtual void Start()
        {
            if (AppServiceProvider == null)
                throw new ArgumentNullException(nameof(AppServiceProvider));

            if (_isStarted)
                throw new InvalidOperationException($"MiCake has already started.");

            var context = new ModuleBearingContext(AppServiceProvider, ModuleContext.AllModules, ApplicationOptions);
            _miCakeModuleBoot.Initialization(context);
        }

        /// <summary>
        /// Shudown micake application
        /// </summary>
        public virtual void ShutDown()
        {
            if (_isShutdown)
                throw new InvalidOperationException($"MiCake has already shutdown.");

            var context = new ModuleBearingContext(AppServiceProvider, ModuleContext.AllModules, ApplicationOptions);
            _miCakeModuleBoot.ShutDown(context);

            _appServiceScope?.Dispose();
        }

        /// <summary>
        /// Initialize micake services
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException($"MiCake has already build common services." +
                                        $"The {nameof(Initialize)} method only can called once!");

            if (_entryType == null)
                throw new NullReferenceException($"Cannot find entry module type,Please marke sure you has already call {nameof(SetEntry)} method.");

            AddMiCakeCoreSerivces(_services);

            //Find all micake modules according to the entry module type
            ModuleManager.PopulateModules(_entryType);

            var logger = _services.BuildServiceProvider().GetService<ILogger<MiCakeModuleBoot>>();
            _miCakeModuleBoot = new MiCakeModuleBoot(logger, ModuleContext.AllModules);
            //auto register services to di.
            _miCakeModuleBoot.AddConfigService(AutoRegisterServices);

            var configServiceContext = new ModuleConfigServiceContext(
                                                _services,
                                                ModuleContext.MiCakeModules,
                                                ApplicationOptions);

            _miCakeModuleBoot.ConfigServices(configServiceContext);
        }

        /// <summary>
        /// Set entry module type.
        /// </summary>
        public void SetEntry(Type type)
        {
            _entryType = type ?? throw new ArgumentException("Please add startUp type when you use AddMiCake().");
        }

        /// <summary>
        /// Set <see cref="IServiceProvider"/>.
        /// </summary>
        void INeedNecessaryParts<IServiceProvider>.SetNecessaryParts([NotNull]IServiceProvider parts)
        {
            _serviceProvider = parts ??
                              throw new ArgumentNullException($"{nameof(IServiceProvider)} cannot be null.");
        }

        private void AddMiCakeCoreSerivces(IServiceCollection services)
        {
            services.AddSingleton<IMiCakeApplication>(this);

            services.AddSingleton<IServiceLocator, ServiceLocator>(provider =>
            {
                ServiceLocator.Instance.Locator = provider;
                return ServiceLocator.Instance;
            });
            services.AddSingleton<IMiCakeErrorHandler, DefaultMiCakeErrorHandler>();
            services.AddSingleton<ILogErrorHandlerProvider, DefaultLogErrorHandlerProvider>();
        }

        //Inject service into container according to matching rules
        private void AutoRegisterServices(ModuleConfigServiceContext context)
        {
            var serviceRegistrar = new DefaultServiceRegistrar(context.Services);
            if (ApplicationOptions.FindAutoServiceTypes != null)
                serviceRegistrar.SetServiceTypesFinder(ApplicationOptions.FindAutoServiceTypes);

            serviceRegistrar.Register(context.MiCakeModules);
        }

        public virtual void Dispose()
        {
            if (!_isShutdown)
                ShutDown();

            ModuleManager = null;
        }
    }
}
