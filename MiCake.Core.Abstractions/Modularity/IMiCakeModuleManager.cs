using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// Manages the modules of an Micake application.
    /// 
    /// 管理Micake应用程序的模块。
    /// </summary>
    public interface IMiCakeModuleManager
    {
        IMiCakeModuleCollection miCakeModules { get; }

        void PopulateDefaultModule(Type startUp);
    }
}
