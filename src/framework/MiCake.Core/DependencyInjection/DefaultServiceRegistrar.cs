using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.Core.DependencyInjection
{
    internal class DefaultServiceRegistrar(IServiceCollection services) : MiCakeServiceRegistrarBase(services)
    {
        public IServiceCollection _services = services;

        protected override List<InjectServiceInfo> AddInjectServices(Type currentType)
        {
            List<InjectServiceInfo> result = [];

            //add attribute mark services
            var injectServiceInfo = currentType.GetCustomAttribute<InjectServiceAttribute>();
            if (injectServiceInfo != null)
            {
                foreach (var exposeService in GetAttributeServices(currentType, injectServiceInfo))
                {
                    result.Add(new InjectServiceInfo()
                    {
                        Type = exposeService,
                        ImplementationType = currentType,
                        Lifetime = injectServiceInfo.Lifetime,
                        ReplaceServices = injectServiceInfo.ReplaceServices,
                        TryRegister = injectServiceInfo.TryRegister
                    });
                }
            }

            //add inherit inteface services 
            var interfaceLifeTime = GetServiceLifetime(currentType);
            if (interfaceLifeTime != null)
            {
                foreach (var exposeService in GetInheritInterfaceServices(currentType))
                {
                    result.Add(new InjectServiceInfo()
                    {
                        Type = exposeService,
                        ImplementationType = currentType,
                        Lifetime = interfaceLifeTime.Value,
                        ReplaceServices = false,
                        TryRegister = false
                    });
                }
            }

            return result;
        }

        private static List<Type> GetAttributeServices(Type type, InjectServiceAttribute serviceAttribute)
            => serviceAttribute.GetServiceTypes(type);

        private List<Type> GetInheritInterfaceServices(Type type)
        {
            if (!typeof(IAutoInjectService).IsAssignableFrom(type))
                return null;

            var currentTypeInterfaces = type.GetInterfaces().AsEnumerable().ToList();

            return CurrentFinder.Invoke(type, currentTypeInterfaces);
        }

    }
}
