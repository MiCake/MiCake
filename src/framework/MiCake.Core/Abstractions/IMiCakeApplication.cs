using MiCake.Core.Modularity;
using System;
using System.Threading.Tasks;

namespace MiCake.Core
{
    /// <summary>
    /// Represents a micake application.
    /// </summary>
    public interface IMiCakeApplication : IAsyncDisposable
    {
        /// <summary>
        /// <see cref="MiCakeApplicationOptions"/>
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; }

        /// <summary>
        /// <see cref="IMiCakeModuleManager"/>
        /// </summary>
        IMiCakeModuleManager ModuleManager { get; }

        /// <summary>
        /// Set entry module to start micake.
        /// </summary>
        /// <param name="type"></param>
        void SetEntry(Type type);

        /// <summary>
        /// Use to build micake modules and trigger config services lifetime.
        /// </summary>
        Task Initialize();

        /// <summary>
        /// Start micake appliction.
        /// </summary>
        Task Start();

        /// <summary>
        /// Used to gracefully shutdown the application and all modules.
        /// </summary>
        Task ShutDown();
    }
}
