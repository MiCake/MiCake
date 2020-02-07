using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

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
