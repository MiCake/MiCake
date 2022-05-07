using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    public interface IMiCakeModuleSorter
    {
        /// <summary>
        /// Custom sort for ordered modules
        /// </summary>
        /// <param name="ordinalModules"></param>
        /// <returns></returns>
        IMiCakeModuleCollection Sort(IMiCakeModuleCollection ordinalModules);
    }
}
