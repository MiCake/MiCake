using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
{
    public interface IMiCakeApplication
    {
        Type StartUpType { get; set; }

        /// <summary>
        /// <see cref=" IMiCakeModuleEngine"/>
        /// </summary>
        IMiCakeModuleEngine ModuleEngine { get; }

        /// <summary>
        /// List of services registered to this application.
        /// Can not add new services to this collection after application initialize.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Used to gracefully shutdown the application and all modules.
        /// </summary>
        void ShutDown(Action<IMiCakeModuleEngine> shutdownAction = null);
    }
}
