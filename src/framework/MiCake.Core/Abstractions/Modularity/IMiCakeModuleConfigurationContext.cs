using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Context for configuring MiCake modules during application setup.
    /// Provides a fluent API for module registration and configuration.
    /// </summary>
    public interface IMiCakeModuleConfigurationContext
    {
        /// <summary>
        /// Register a module with optional configuration
        /// </summary>
        /// <typeparam name="TModule">The module type</typeparam>
        /// <param name="configureModule">Optional module configuration action</param>
        void AddModule<TModule>(Action<object>? configureModule = null) where TModule : MiCakeModule;

        /// <summary>
        /// Register a module by type with optional configuration
        /// </summary>
        /// <param name="moduleType">The module type</param>
        /// <param name="configureModule">Optional module configuration action</param>
        void AddModule(Type moduleType, Action<object>? configureModule = null);

        /// <summary>
        /// Get the application options for configuration
        /// </summary>
        MiCakeApplicationOptions Options { get; }
    }
}
