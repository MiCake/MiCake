using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
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
