﻿using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.Core.DependencyInjection
{
    internal abstract class MiCakeServiceRegistrarBase : IMiCakeServiceRegistrar
    {
        private readonly IServiceCollection _services;
        private FindAutoServiceTypesDelegate _serviceTypesFinder;

        protected FindAutoServiceTypesDelegate CurrentFinder => _serviceTypesFinder ?? DefaultFindServiceTypes.Finder;

        public MiCakeServiceRegistrarBase(IServiceCollection serviceCollection)
        {
            _services = serviceCollection;
        }

        public virtual Task Register(IMiCakeModuleCollection miCakeModules)
        {
            var injectServices = new List<InjectServiceInfo>();

            //filter need register modules
            var needRegitsterModules = miCakeModules.Where(s => s.Instance.IsAutoRegisterServices)
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
                var serviceLifetime = serviceInfo.Lifetime.HasValue ?
                                            serviceInfo.Lifetime.Value.ConvertToMSLifetime() :
                                            ServiceLifetime.Transient;

                var serviceDescriptor = new ServiceDescriptor(serviceInfo.Type, serviceInfo.ImplementationType, serviceLifetime);

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

            return Task.CompletedTask;
        }

        protected abstract List<InjectServiceInfo> AddInjectServices(Type currentType);

        public Task SetServiceTypesFinder(FindAutoServiceTypesDelegate findAutoServiceTypes)
        {
            _serviceTypesFinder = findAutoServiceTypes;

            return Task.CompletedTask;
        }

        protected virtual MiCakeServiceLifetime? GetServiceLifetime(Type type)
        {
            if (typeof(ITransientService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifetime.Transient;
            }

            if (typeof(ISingletonService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifetime.Singleton;
            }

            if (typeof(IScopedService).IsAssignableFrom(type))
            {
                return MiCakeServiceLifetime.Scoped;
            }

            return null;
        }

        #region InjectServiceInfo
        protected class InjectServiceInfo
        {
            public Type Type { get; set; }

            public Type ImplementationType { get; set; }

            public MiCakeServiceLifetime? Lifetime { get; set; }

            public bool TryRegister { get; set; }

            public bool ReplaceServices { get; set; }
        }
        #endregion
    }
}
