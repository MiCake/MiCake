using MiCake.Core.Modularity;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Represents a MiCake application instance.
    /// The application manages the module system and coordinates the application lifecycle.
    /// </summary>
    public interface IMiCakeApplication : IDisposable
    {
        /// <summary>
        /// Gets the application options
        /// </summary>
        MiCakeApplicationOptions ApplicationOptions { get; }

        /// <summary>
        /// Gets the module context (read-only access to loaded modules)
        /// </summary>
        IMiCakeModuleContext ModuleContext { get; }

        /// <summary>
        /// Starts the MiCake application.
        /// This executes the initialization lifecycle of all modules.
        /// </summary>
        void Start();

        /// <summary>
        /// Gracefully shuts down the application and all modules.
        /// This executes the shutdown lifecycle of all modules in reverse order.
        /// </summary>
        void ShutDown();
    }
}
