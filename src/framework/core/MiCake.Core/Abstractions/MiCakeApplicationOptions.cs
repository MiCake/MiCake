using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using System.Reflection;

namespace MiCake.Core
{
    /// <summary>
    /// The configuration of building the core program of micake
    /// </summary>
    public class MiCakeApplicationOptions
    {
        /// <summary>
        /// Specifies a custom module sorter that can change the startup order of MiCake application modules.
        /// </summary>
        public IMiCakeModuleSorter? ModuleSorter { get; set; }

        /// <summary>
        /// Configuration items for auto injection service
        /// 
        /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
        /// or <see cref="IScopedService"/> interface will be injected automatically.
        /// But we need to determine which type of service this class is.
        /// 
        /// defalut: find class all interfaces.The service whose interface name contains the class name.
        /// </summary>
        public FindAutoServiceTypesDelegate FindAutoServiceTypes { get; set; }

        /// <summary>
        /// Assemblies of domain layer
        /// Providing this parameter will facilitate micake to better scan related domain objects in the program.
        /// </summary>
        public Assembly[] DomainLayerAssemblies { get; set; }


        /// <summary>
        /// Use given option value.
        /// </summary>
        /// <param name="applicationOptions"></param>
        public void Apply(MiCakeApplicationOptions applicationOptions)
        {
            FindAutoServiceTypes = applicationOptions.FindAutoServiceTypes;
            DomainLayerAssemblies = applicationOptions.DomainLayerAssemblies;
            ModuleSorter = applicationOptions.ModuleSorter;
        }
    }
}
