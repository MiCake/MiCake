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
using System;

namespace MiCake.Core
{
    public class MiCakeApplication : IMiCakeApplication
    {
        public Type StartUpType { get; }
        public IMiCakeBuilder Builder { get; private set; }

        private IServiceProvider _serviceProvider;
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

            //add micake core serivces
            AddMiCakeCoreSerivces(services);

            var moduleManager = new MiCakeModuleManager();
            Builder = new MiCakeBuilder(services, moduleManager);

            //populate normal and feature modules
            moduleManager.PopulateModules(_startUp);

            builderConfigAction?.Invoke(Builder);

            moduleManager.ConfigServices(services, modules =>
            {
                //auto inject service to the collection.
                var serviceRegistrar = new DefaultServiceRegistrar(services);
                if (options.FindAutoServiceTypes != null)
                    serviceRegistrar.SetServiceTypesFinder(options.FindAutoServiceTypes);

                serviceRegistrar.Register(modules);
            });

            services.AddSingleton(Builder);
        }

        public virtual void Init()
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(_serviceProvider));

            var moduleBoot = _serviceProvider.GetRequiredService<IMiCakeModuleBoot>();
            moduleBoot.Initialization(new ModuleBearingContext(_serviceProvider, Builder.ModuleManager.MiCakeModules));
        }

        public virtual void ShutDown(Action<ModuleBearingContext> shutdownAction = null)
        {
            if (_serviceProvider == null)
                throw new ArgumentNullException(nameof(ServiceProvider));

            var moduleBoot = _serviceProvider.GetRequiredService<IMiCakeModuleBoot>();
            var context = new ModuleBearingContext(_serviceProvider, Builder.ModuleManager.MiCakeModules);
            shutdownAction?.Invoke(context);
            moduleBoot.ShutDown(context);
        }

        public virtual void Dispose()
        {
            Builder = null;
        }

        protected virtual IMiCakeApplication SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            return this;
        }

        private void AddMiCakeCoreSerivces(IServiceCollection services)
        {
            services.AddSingleton<IMiCakeModuleBoot, MiCakeModuleBoot>();
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
