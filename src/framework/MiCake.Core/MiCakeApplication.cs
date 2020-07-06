using MiCake.Core.Data;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace MiCake.Core
{
    public class MiCakeApplication : IMiCakeApplication, INeedParts<IServiceProvider>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
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

        private bool _isInitialized = false;
        private bool _isStarted = false;
        private bool _isShutdown = false;

        public MiCakeApplication(
            IServiceCollection services,
            MiCakeApplicationOptions options,
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

            //Pre activation ServiceLocator
            AppServiceProvider.GetService(typeof(IServiceLocator));

            //Module Inspection.
            var inspectContext = new ModuleInspectionContext(AppServiceProvider, ModuleContext.AllModules);
            foreach (var module in ModuleContext.AllModules)
            {
                if (module is IModuleSelfInspection selfInspection)
                    selfInspection.Inspect(inspectContext);
            }

            var context = new ModuleLoadContext(AppServiceProvider, ModuleContext.AllModules, ApplicationOptions);
            _miCakeModuleBoot.Initialization(context);

            //Release options additional infomation.
            ApplicationOptions.AdditionalInfo.Release();
        }

        /// <summary>
        /// Shudown micake application
        /// </summary>
        public virtual void ShutDown()
        {
            if (_isShutdown)
                throw new InvalidOperationException($"MiCake has already shutdown.");

            var context = new ModuleLoadContext(AppServiceProvider, ModuleContext.AllModules, ApplicationOptions);
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
                throw new NullReferenceException($"Cannot find entry module type,Please make sure you has already call {nameof(SetEntry)} method.");

            AddMiCakeCoreSerivces(_services);

            //Find all micake modules according to the entry module type
            ModuleManager.PopulateModules(_entryType);
            _services.AddSingleton(ModuleContext);

            var loggerFactory = _services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                                ?? NullLoggerFactory.Instance;

            _miCakeModuleBoot = new MiCakeModuleBoot(loggerFactory, ModuleContext.AllModules);
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
            _entryType = type ?? throw new ArgumentException("Please add startup type when you use AddMiCake().");
        }

        /// <summary>
        /// Set <see cref="IServiceProvider"/>.
        /// </summary>
        void INeedParts<IServiceProvider>.SetParts(IServiceProvider parts)
        {
            _serviceProvider = parts ??
                              throw new ArgumentNullException($"{nameof(IServiceProvider)} cannot be null.");
        }

        private void AddMiCakeCoreSerivces(IServiceCollection services)
        {
            services.AddSingleton<IMiCakeApplication>(this);
            services.Configure<MiCakeApplicationOptions>(op => op.Apply(ApplicationOptions));

            services.AddSingleton<IServiceLocator, ServiceLocator>(provider =>
            {
                ServiceLocator.Instance.Locator = provider;
                return ServiceLocator.Instance;
            });
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
