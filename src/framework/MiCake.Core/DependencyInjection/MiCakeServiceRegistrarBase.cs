using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.DependencyInjection
{
    internal abstract class MiCakeServiceRegistrarBase : IMiCakeServiceRegistrar
    {
        private IServiceCollection _services;
        private FindAutoServiceTypesDelegate _serviceTypesFinder;

        protected FindAutoServiceTypesDelegate CurrentFinder => _serviceTypesFinder ?? DefaultFindServiceTypes.Finder;

        public MiCakeServiceRegistrarBase(IServiceCollection serviceCollection)
        {
            _services = serviceCollection;
        }

        public virtual void Register(IMiCakeModuleCollection miCakeModules)
        {
            var injectServices = new List<InjectServiceInfo>();

            //filter need register modules
            var needRegitsterModules = miCakeModules.Where(s => s.ModuleInstance.IsAutoRegisterServices)
                                                    .ToMiCakeModuleCollection();

            var assemblies = needRegitsterModules.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(type => type.IsClass & !type.IsAbstract & !type.IsSealed).ToList();

                foreach (var type in types)
                {
                    var currentTypeServices = AddInjectServices(type);
                    if (currentTypeServices.Count != 0)
                        injectServices.AddRange(currentTypeServices);
                }
            }

            foreach (var serviceInfo in injectServices)
            {
                var serviceDescriptor = serviceInfo.Lifetime switch
                {
                    MiCakeServiceLifeTime.Singleton => new ServiceDescriptor(serviceInfo.Type, serviceInfo.ImplementationType, ServiceLifetime.Singleton),
                    MiCakeServiceLifeTime.Transient => new ServiceDescriptor(serviceInfo.Type, serviceInfo.ImplementationType, ServiceLifetime.Transient),
                    MiCakeServiceLifeTime.Scoped => new ServiceDescriptor(serviceInfo.Type, serviceInfo.ImplementationType, ServiceLifetime.Scoped),
                    _ => new ServiceDescriptor(serviceInfo.Type, serviceInfo.ImplementationType, ServiceLifetime.Transient)
                };

                if (serviceInfo.ReplaceServices)
                {
                    _services.Replace(serviceDescriptor);
                }
                else if (serviceInfo.TryRegister)
                {
                    _services.TryAdd(serviceDescriptor);
                }
                else
                {
                    _services.Add(serviceDescriptor);
                }
            }
        }

        protected abstract List<InjectServiceInfo> AddInjectServices(Type currentType);

        public void SetServiceTypesFinder(FindAutoServiceTypesDelegate findAutoServiceTypes)
        {
            _serviceTypesFinder = findAutoServiceTypes;
        }

        protected virtual MiCakeServiceLifeTime? GetServiceLifetime(Type type)
        {
            if (typeof(ITransientService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Transient;
            }

            if (typeof(ISingletonService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Singleton;
            }

            if (typeof(IScopedService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Scoped;
            }

            return null;
        }

        #region InjectServiceInfo
        protected class InjectServiceInfo
        {
            public Type Type { get; set; }

            public Type ImplementationType { get; set; }

            public MiCakeServiceLifeTime? Lifetime { get; set; }

            public bool TryRegister { get; set; }

            public bool ReplaceServices { get; set; }
        }
        #endregion
    }
}
