using MiCake.Core.DependencyInjection;
using MiCake.Util.Store;
using System.Reflection;

namespace MiCake.Core
{
    /// <summary>
    /// The configuration of building the core program of micake
    /// </summary>
    public class MiCakeApplicationOptions
    {
        /// <summary>
        /// Configuration items for auto injection service
        /// 
        /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
        /// or <see cref="IScopedService"/> interface will be injected automatically.
        /// But we need to determine which type of service this class is.
        /// 
        /// default: find class all interfaces. The service whose interface name contains the class name.
        /// </summary>
        public ServiceTypeDiscoveryHandler? FindAutoServiceTypes { get; set; }

        /// <summary>
        /// Assemblies of domain layer
        /// Providing this parameter will facilitate micake to better scan related domain objects in the program.
        /// </summary>
        public Assembly[]? DomainLayerAssemblies { get; set; }

        /// <summary>
        /// Configuration options for printing module information during initialization.
        /// </summary>
        public PrintingOptions Printing { get; set; } = new PrintingOptions();

        /// <summary>
        /// A data stash that only exists during the build process.
        /// It can be used to store data cross modules during the build phase.
        /// </summary>
        public DataDepositPool BuildPhaseData { get; set; } = new DataDepositPool();

        /// <summary>
        /// Use given option value.
        /// </summary>
        /// <param name="applicationOptions"></param>
        public void Apply(MiCakeApplicationOptions applicationOptions)
        {
            FindAutoServiceTypes = applicationOptions.FindAutoServiceTypes;
            DomainLayerAssemblies = applicationOptions.DomainLayerAssemblies;
            Printing = applicationOptions.Printing ?? new PrintingOptions();
            BuildPhaseData = applicationOptions.BuildPhaseData;
        }


        /// <summary>
        /// Configuration options for printing module information during initialization.
        /// </summary>
        public class PrintingOptions
        {
            /// <summary>
            /// Whether to print the welcome brand string during module initialization.
            /// Default is true.
            /// </summary>
            public bool WelcomeBrand { get; set; } = true;

            /// <summary>
            /// Whether to print the module dependency graph during module initialization.
            /// Default is true.
            /// </summary>
            public bool DependencyGraph { get; set; } = true;

            /// <summary>
            /// Creates a new instance of PrintingOptions with all printing enabled.
            /// </summary>
            /// <returns>A new PrintingOptions instance with all features enabled</returns>
            public static PrintingOptions EnableAll()
            {
                return new PrintingOptions
                {
                    WelcomeBrand = true,
                    DependencyGraph = true
                };
            }

            /// <summary>
            /// Creates a new instance of PrintingOptions with all printing disabled.
            /// </summary>
            /// <returns>A new PrintingOptions instance with all features disabled</returns>
            public static PrintingOptions DisableAll()
            {
                return new PrintingOptions
                {
                    WelcomeBrand = false,
                    DependencyGraph = false
                };
            }
        }
    }
}
