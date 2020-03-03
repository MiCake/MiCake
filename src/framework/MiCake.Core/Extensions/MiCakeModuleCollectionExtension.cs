using MiCake.Core.Modularity;
using System.Collections.Generic;

namespace MiCake.Core.Extensions
{
    public static class MiCakeModuleCollectionExtension
    {
        public static IMiCakeModuleCollection ToMiCakeModuleCollection(this IEnumerable<MiCakeModuleDescriptor> source)
        {
            MiCakeModuleCollection miCakeModules = new MiCakeModuleCollection();

            foreach (var item in source)
            {
                miCakeModules.Add(item);
            }

            return miCakeModules;
        }
    }
}
