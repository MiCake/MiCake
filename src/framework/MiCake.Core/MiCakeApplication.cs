using MiCake.Core.Builder;
using MiCake.Core.DependencyInjection;
using MiCake.Core.ExceptionHandling;
using MiCake.Core.Logging;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace MiCake.Core
{
    public class MiCakeApplication : IMiCakeApplication
    {
        public Type StartUpType { get; }
        public IMiCakeBuilder Builder { get; private set; }
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        public IMiCakeModuleManager ModuleManager
        {
            get
            {
                return null;
            }
        }

        private IMiCakeModuleBoot _miCakeModuleBoot;
        private IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;
        private IServiceScope _appServiceScope;
        private Type _startUp;
        private bool _isBuildCommonService;

        public MiCakeApplication(
            IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            ApplicationOptions = options;

            AddMiCakeCoreSerivces(services);
        }

        public virtual void Start()
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(_serviceProvider));

            _appServiceScope = _serviceProvider.CreateScope();

            var scopedServiceProvider = _appServiceScope.ServiceProvider;
            var context = new ModuleBearingContext(scopedServiceProvider, Builder.ModuleManager.AllModules, ApplicationOptions);
            _miCakeModuleBoot.Initialization(context);
        }

        public virtual void ShutDown()
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(ServiceProvider));

            var scopedServiceProvider = _appServiceScope.ServiceProvider;

            var context = new ModuleBearingContext(scopedServiceProvider, Builder.ModuleManager.MiCakeModules, ApplicationOptions);
            _miCakeModuleBoot.ShutDown(context);

            _appServiceScope.Dispose();
        }

        public virtual void Dispose()
        {
            Builder = null;
            _miCakeModuleBoot = null;
            _serviceProvider = null;
        }

        protected virtual IMiCakeApplication SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            return this;
        }

        private void AddMiCakeCoreSerivces(IServiceCollection services)
        {
            services.AddSingleton<IServiceLocator, ServiceLocator>(provider =>
            {
                ServiceLocator.Instance.Locator = provider;
                return ServiceLocator.Instance;
            });
            services.AddSingleton<IMiCakeErrorHandler, DefaultMiCakeErrorHandler>();
            services.AddSingleton<ILogErrorHandlerProvider, DefaultLogErrorHandlerProvider>();
        }

        private void PopulateModules(out IMiCakeModuleManager moduleManager)
        {
            var manager = new MiCakeModuleManager();
            manager.PopulateModules(_startUp);

            moduleManager = manager;
        }

        public void SetEntry(Type type)
        {
            _startUp = type ?? throw new ArgumentException("Please add startUp type when you use AddMiCake().");
        }

        public void Initialize()
        {
            if (_isBuildCommonService)
                throw new InvalidOperationException($"MiCake has already build common services.The {nameof(Initialize)} method only can called once!");

            PopulateModules(out var moduleManager);

            Builder = new MiCakeBuilder(_services, moduleManager);

            _miCakeModuleBoot = new MiCakeModuleBoot(
                _services.BuildServiceProvider().GetService<ILogger<MiCakeModuleBoot>>(),
                Builder);

            var configServiceContext = new ModuleConfigServiceContext(_services, moduleManager.AllModules, ApplicationOptions);
            _miCakeModuleBoot.ConfigServices(configServiceContext, AutoRegisterServices);

            void AutoRegisterServices(ModuleConfigServiceContext context)
            {
                var serviceRegistrar = new DefaultServiceRegistrar(context.Services);
                if (ApplicationOptions.FindAutoServiceTypes != null)
                    serviceRegistrar.SetServiceTypesFinder(ApplicationOptions.FindAutoServiceTypes);

                serviceRegistrar.Register(context.MiCakeModules);
            }
        }
    }
}
