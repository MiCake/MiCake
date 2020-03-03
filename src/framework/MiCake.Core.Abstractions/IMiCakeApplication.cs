using MiCake.Core.Builder;
using MiCake.Core.Modularity;
using System;

namespace MiCake.Core
{
    public interface IMiCakeApplication : IDisposable
    {
        Type StartUpType { get; }

        IMiCakeBuilder Builder { get; }

        void Init();

        /// <summary>
        /// Used to gracefully shutdown the application and all modules.
        /// </summary>
        void ShutDown(Action<ModuleBearingContext> shutdownAction = null);
    }
}
