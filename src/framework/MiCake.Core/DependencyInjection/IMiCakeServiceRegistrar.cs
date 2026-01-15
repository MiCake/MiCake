using System.Threading.Tasks;
using MiCake.Core.Modularity;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Manages automatic service registration in MiCake framework.
    /// Scans modules and registers services marked with <see cref="InjectServiceAttribute"/> 
    /// or implementing marker interfaces (<see cref="ITransientService"/>, <see cref="IScopedService"/>, <see cref="ISingletonService"/>).
    /// </summary>
    internal interface IMiCakeServiceRegistrar
    {
        /// <summary>
        /// Registers all services from the given module collection that have automatic registration enabled.
        /// </summary>
        /// <param name="miCakeModules">Collection of MiCake modules to scan for services</param>
        /// <returns>A task that represents the asynchronous registration operation</returns>
        Task Register(IMiCakeModuleCollection miCakeModules);

        /// <summary>
        /// Sets a custom service type finder for automatic service registration.
        /// This allows customization of how service types are discovered from implementation types.
        /// </summary>
        /// <param name="findAutoServiceTypes">The delegate that determines which service types to register</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SetServiceTypesFinder(ServiceTypeDiscoveryHandler findAutoServiceTypes);
    }
}
