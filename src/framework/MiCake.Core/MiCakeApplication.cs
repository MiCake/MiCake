using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.ExceptionHandling;
using MiCake.Core.Abstractions.Logging;
using MiCake.Core.Abstractions.Modularity;
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

        private IMiCakeModuleBoot _miCakeModuleBoot;
        private IServiceProvider _serviceProvider;
        private IServiceScope _appServiceScope;
        private Type _startUp;

        public MiCakeApplication(
            Type startUp,
            IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            if (startUp == null)
                throw new ArgumentException("Please add startUp type when you use AddMiCake().");

            _startUp = startUp;
            ApplicationOptions = options;

            //add micake core serivces
            AddMiCakeCoreSerivces(services);

            var moduleManager = new MiCakeModuleManager();
            Builder = new MiCakeBuilder(services, moduleManager);

            //populate normal and feature modules
            moduleManager.PopulateModules(_startUp);

            builderConfigAction?.Invoke(Builder);

            moduleManager.ConfigServices(services, AutoRegisterServices);
        }

        public virtual void Init()
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(_serviceProvider));

            _appServiceScope = _serviceProvider.CreateScope();

            var scopedServiceProvider = _appServiceScope.ServiceProvider;
            _miCakeModuleBoot = new MiCakeModuleBoot(scopedServiceProvider.GetService<ILogger<MiCakeModuleBoot>>(), Builder);

            var context = new ModuleBearingContext(scopedServiceProvider, Builder.ModuleManager.MiCakeModules);
            _miCakeModuleBoot.Initialization(context);
        }

        public virtual void ShutDown(Action<ModuleBearingContext> shutdownAction = null)
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(ServiceProvider));

            var scopedServiceProvider = _appServiceScope.ServiceProvider;

            var context = new ModuleBearingContext(scopedServiceProvider, Builder.ModuleManager.MiCakeModules);
            shutdownAction?.Invoke(context);
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

        private void AutoRegisterServices(IServiceCollection services, IMiCakeModuleCollection miCakeModules)
        {
            var serviceRegistrar = new DefaultServiceRegistrar(services);
            if (ApplicationOptions.FindAutoServiceTypes != null)
                serviceRegistrar.SetServiceTypesFinder(ApplicationOptions.FindAutoServiceTypes);

            serviceRegistrar.Register(miCakeModules);
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
    }
}
