using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MiCake.Core.Util.Collections;
using MiCake.Core.Util;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Core.DependencyInjection
{
    public class DefaultMiCakeDIManager : BaseMiCakeDIManager
    {
        public DefaultMiCakeDIManager(IServiceCollection services) : base(services)
        {
        }

        public override void PopulateAutoService(IMiCakeModuleCollection miCakeModules)
        {
            var injectServices = new List<InjectServiceInfo>();

            var assemblies = miCakeModules.GetAllReferAssembly();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(type => type.IsClass & !type.IsAbstract).ToList();

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
                    Type = type,
                    ImplementationType = injectServiceAttribute.Type,
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
