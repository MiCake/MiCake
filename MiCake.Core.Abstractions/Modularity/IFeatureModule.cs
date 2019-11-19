using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// Feature Module:Unlike normal modules, they may be independent of the framework.
    /// And there is no obvious dependency framework.
    /// Provide more extension functions for the framework in pluggable form
    /// </summary>
    public interface IFeatureModule
    {
        /// <summary>
        /// need before or after normal module to start
        /// </summary>
        FeatureModuleLoadOrder Order { get; }

        /// <summary>
        /// need auto register when micake init
        /// </summary>
        bool AutoRegisted { get; set; }
    }
}
