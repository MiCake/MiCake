using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Context for module initialization phase.
    /// Provides access to ServiceProvider and other modules during initialization.
    /// </summary>
    public class ModuleInitializationContext
    {
        /// <summary>
        /// Gets the service provider for resolving dependencies
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the collection of all registered modules
        /// </summary>
        public IMiCakeModuleCollection Modules { get; }

        /// <summary>
        /// Gets the application options
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; }

        /// <summary>
        /// Creates a new module initialization context
        /// </summary>
        public ModuleInitializationContext(
            IServiceProvider serviceProvider,
            IMiCakeModuleCollection modules,
            MiCakeApplicationOptions applicationOptions)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Modules = modules ?? throw new ArgumentNullException(nameof(modules));
            ApplicationOptions = applicationOptions ?? throw new ArgumentNullException(nameof(applicationOptions));
        }
    }
}
