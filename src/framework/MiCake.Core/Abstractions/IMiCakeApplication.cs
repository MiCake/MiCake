using MiCake.Core.Modularity;
using System;
using System.Threading.Tasks;

namespace MiCake.Core
{
    /// <summary>
    /// Represents a MiCake application instance.
    /// The application manages the module system and coordinates the application lifecycle.
    /// </summary>
    public interface IMiCakeApplication : IAsyncDisposable
    {
        /// <summary>
        /// Gets the application options
        /// </summary>
        MiCakeApplicationOptions ApplicationOptions { get; }

        /// <summary>
        /// Gets the module manager responsible for module discovery and lifecycle
        /// </summary>
        IMiCakeModuleManager ModuleManager { get; }

        /// <summary>
        /// Sets the entry module type to start MiCake application.
        /// The entry module serves as the root of the module dependency tree.
        /// </summary>
        /// <param name="type">The entry module type</param>
        void SetEntry(Type type);

        /// <summary>
        /// Initializes the MiCake application.
        /// This discovers and configures all modules and their dependencies.
        /// Must be called before <see cref="Start"/>.
        /// </summary>
        Task Initialize();

        /// <summary>
        /// Starts the MiCake application.
        /// This executes the initialization lifecycle of all modules.
        /// Must be called after <see cref="Initialize"/>.
        /// </summary>
        Task Start();

        /// <summary>
        /// Gracefully shuts down the application and all modules.
        /// This executes the shutdown lifecycle of all modules in reverse order.
        /// </summary>
        Task ShutDown();
    }
}
