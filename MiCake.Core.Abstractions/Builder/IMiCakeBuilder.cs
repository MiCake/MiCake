using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Builder
{
    public interface IMiCakeBuilder
    {
        /// <summary>
        /// <see cref="IMiCakeModuleEngine"/>
        /// </summary>
        IMiCakeModuleEngine ModuleEngine { get; }

        /// <summary>
        /// Declaration starting entry point
        /// </summary>
        IMiCakeBuilder UseStarpUp(Type startUp);

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        void Build();
    }
}
