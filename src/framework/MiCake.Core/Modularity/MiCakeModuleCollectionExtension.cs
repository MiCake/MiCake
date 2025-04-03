using System.Collections.Generic;

namespace MiCake.Core.Modularity
{
    public static class MiCakeModuleCollectionExtension
    {
        public static IMiCakeModuleCollection ToMiCakeModuleCollection(this IEnumerable<MiCakeModuleDescriptor> source)
        {
            MiCakeModuleCollection miCakeModules = [.. source];

            return miCakeModules;
        }
    }
}
