using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Base implementation for service registration in MiCake framework.
    /// Handles the core logic of scanning assemblies and registering services.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MiCakeServiceRegistrarBase"/> class.
    /// </remarks>
    /// <param name="serviceCollection">The service collection to register services into</param>
    internal abstract class MiCakeServiceRegistrarBase(IServiceCollection serviceCollection) : IMiCakeServiceRegistrar
    {
        private readonly IServiceCollection _services = serviceCollection;
        private ServiceTypeDiscoveryHandler? _serviceTypesFinder;

        /// <summary>
        /// Gets the current service type finder, using the default if none is set.
        /// </summary>
        protected ServiceTypeDiscoveryHandler CurrentFinder => _serviceTypesFinder ?? DefaultFindServiceTypes.Finder;

        /// <summary>
        /// Registers all services from modules that have automatic registration enabled.
        /// Scans all types in the module assemblies and registers those marked for automatic injection.
        /// </summary>
        /// <param name="miCakeModules">Collection of MiCake modules to scan</param>
        /// <returns>A completed task</returns>
        public virtual Task Register(IMiCakeModuleCollection miCakeModules)
        {
            var injectServices = new List<InjectServiceInfo>();

            // Filter modules that have automatic service registration enabled
            var needRegisterModules = miCakeModules.Where(s => s.Instance.EnableAutoServiceRegistration)
                                                    .ToMiCakeModuleCollection();

            var assemblies = needRegisterModules.GetAssemblies(true);
            foreach (var assembly in assemblies)
            {
                // Only scan non-sealed classes (sealed classes cannot be derived from)
                var types = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && !type.IsSealed).ToList();

                foreach (var type in types)
                {
                    var currentTypeServices = AddInjectServices(type);
                    if (currentTypeServices.Count != 0)
                        injectServices.AddRange(currentTypeServices);
                }
            }

            // Register all discovered services
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

        /// <summary>
        /// Analyzes a type and returns the services that should be registered for it.
        /// Derived classes must implement this to define specific registration logic.
        /// </summary>
        /// <param name="currentType">The type to analyze</param>
        /// <returns>List of service registration information</returns>
        protected abstract List<InjectServiceInfo> AddInjectServices(Type currentType);

        /// <summary>
        /// Sets a custom service type finder delegate.
        /// </summary>
        /// <param name="findAutoServiceTypes">The custom finder delegate</param>
        /// <returns>A completed task</returns>
        public Task SetServiceTypesFinder(ServiceTypeDiscoveryHandler findAutoServiceTypes)
        {
            _serviceTypesFinder = findAutoServiceTypes;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines the service lifetime based on marker interfaces.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>The service lifetime, or null if no marker interface is found</returns>
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
        /// <summary>
        /// Contains information about a service to be registered.
        /// </summary>
        protected class InjectServiceInfo
        {
            /// <summary>
            /// Gets or sets the service type (typically an interface).
            /// </summary>
            public required Type Type { get; set; }

            /// <summary>
            /// Gets or sets the implementation type (concrete class).
            /// </summary>
            public required Type ImplementationType { get; set; }

            /// <summary>
            /// Gets or sets the service lifetime.
            /// </summary>
            public MiCakeServiceLifetime? Lifetime { get; set; }

            /// <summary>
            /// Gets or sets whether to only register if not already registered.
            /// </summary>
            public bool TryRegister { get; set; }

            /// <summary>
            /// Gets or sets whether to replace existing registrations.
            /// </summary>
            public bool ReplaceServices { get; set; }
        }
        #endregion
    }
}
