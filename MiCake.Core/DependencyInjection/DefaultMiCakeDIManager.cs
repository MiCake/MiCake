using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Core.DependencyInjection
{
    internal class DefaultMiCakeDIManager : IMiCakeDIManager
    {
        public IServiceCollection _services;

        public DefaultMiCakeDIManager(IServiceCollection services)
        {
            _services = services;
        }

        public virtual void PopulateAutoService(IMiCakeModuleCollection miCakeModules)
        {
            var injectServices = new List<InjectServiceInfo>();

            var assemblies = miCakeModules.GetAllReferAssembly();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(type => type.IsClass & !type.IsAbstract & !type.IsSealed).ToList();

                foreach (var type in types)
                {
                    var attrService = GetAttritubeMarkService(type);
                    if (attrService != null) injectServices.Add(attrService);

                    var interfaceService = GetInterfaceMarkService(type);
                    if (interfaceService != null) injectServices.Add(interfaceService);
                }
            }

            injectServices = injectServices.Distinct(new ServiceTypeComparer()).ToList();

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

        /// <summary>
        /// Get services marked in the MiCake framework that need auto injection feature
        /// for example : <see cref="InjectServiceAttribute"/>
        /// </summary>
        protected virtual InjectServiceInfo GetAttritubeMarkService(Type type)
        {
            InjectServiceInfo serviceInfo = null;

            var injectServiceAttribute = type.GetCustomAttribute<InjectServiceAttribute>();
            if (injectServiceAttribute != null)
            {
                serviceInfo = new InjectServiceInfo()
                {
                    Type = injectServiceAttribute.Type ?? type,
                    ImplementationType = type,
                    ReplaceServices = injectServiceAttribute.ReplaceServices,
                    Lifetime = injectServiceAttribute.Lifetime,
                    TryRegister = injectServiceAttribute.TryRegister,
                };
            }

            return serviceInfo;
        }

        /// <summary>
        /// Get services active interface in the MiCake framework that need auto injection feature
        /// for example : <see cref="IAutoInjectService"/>
        /// </summary>
        protected virtual InjectServiceInfo GetInterfaceMarkService(Type type)
        {
            InjectServiceInfo serviceInfo = null;

            if (typeof(IAutoInjectService).IsAssignableFrom(type))
            {
                var serviceLifeTime = GetServiceLifetime(type);

                serviceInfo = new InjectServiceInfo()
                {
                    Type = type,
                    ImplementationType = type,
                    ReplaceServices = false,
                    Lifetime = serviceLifeTime,
                    TryRegister = true
                };
            }

            return serviceInfo;
        }

        //根据type继承的接口类型返回服务生命周期
        protected virtual MiCakeServiceLifeTime? GetServiceLifetime(Type type)
        {
            if (typeof(ITransientService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Transient;
            }

            if (typeof(ISingletonService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Singleton;
            }

            if (typeof(IScopedService).GetTypeInfo().IsAssignableFrom(type))
            {
                return MiCakeServiceLifeTime.Scoped;
            }

            return null;
        }

        protected class InjectServiceInfo
        {
            public Type Type { get; set; }

            public Type ImplementationType { get; set; }

            public MiCakeServiceLifeTime? Lifetime { get; set; }

            public bool TryRegister { get; set; }

            public bool ReplaceServices { get; set; }
        }

        protected class ServiceTypeComparer : IEqualityComparer<InjectServiceInfo>
        {
            public bool Equals(InjectServiceInfo x, InjectServiceInfo y)
            {
                if (x == null)
                    return y == null;
                return x.Type == y.Type;
            }

            public int GetHashCode(InjectServiceInfo obj)
            {
                if (obj == null)
                    return 0;
                return obj.Type.GetHashCode();
            }
        }
    }
}
