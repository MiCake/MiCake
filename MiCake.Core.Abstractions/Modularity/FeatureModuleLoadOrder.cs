using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public enum FeatureModuleLoadOrder
    {
        /// <summary>
        /// Before the common module
        /// </summary>
        BeforeCommonModule,

        /// <summary>
        /// After the common module
        /// </summary>
        AfterCommonModule,
    }
}
