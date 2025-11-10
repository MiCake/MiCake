using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Default implementation of service registration in MiCake framework.
    /// Handles registration of services marked with <see cref="InjectServiceAttribute"/> 
    /// and those implementing marker interfaces.
    /// </summary>
    internal class DefaultServiceRegistrar(IServiceCollection services) : MiCakeServiceRegistrarBase(services)
    {
        /// <summary>
        /// Analyzes a type and determines which services should be registered for it.
        /// Checks both attribute-based registration and marker interface-based registration.
        /// </summary>
        /// <param name="currentType">The type to analyze</param>
        /// <returns>List of services to register</returns>
        protected override List<InjectServiceInfo> AddInjectServices(Type currentType)
        {
            List<InjectServiceInfo> result = [];

            // Add services marked with InjectServiceAttribute
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

            // Add services based on marker interfaces (ITransientService, IScopedService, ISingletonService)
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

        /// <summary>
        /// Gets the service types from an InjectServiceAttribute.
        /// </summary>
        /// <param name="type">The implementation type</param>
        /// <param name="serviceAttribute">The attribute containing service type information</param>
        /// <returns>List of service types to register</returns>
        private static List<Type> GetAttributeServices(Type type, InjectServiceAttribute serviceAttribute)
            => serviceAttribute.GetServiceTypes(type);

        /// <summary>
        /// Gets the service types from inherited interfaces using the configured finder.
        /// Only returns types if the class implements IAutoInjectService marker interface.
        /// </summary>
        /// <param name="type">The implementation type</param>
        /// <returns>List of service types to register, or null if not an auto-inject service</returns>
        private List<Type> GetInheritInterfaceServices(Type type)
        {
            if (!typeof(IAutoInjectService).IsAssignableFrom(type))
                return null;

            var currentTypeInterfaces = type.GetInterfaces().AsEnumerable().ToList();

            return CurrentFinder.Invoke(type, currentTypeInterfaces);
        }
    }
}
