using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Autofac
{
    public static class MiCakeApplicationAutofacExtension
    {
        public static IMiCakeBuilder UseAutofac(this IMiCakeBuilder builder)
        {
            builder.ModuleManager.AddFeatureModule(new MiCakeAutofacModule() { AutoRegisted = true, Order = FeatureModuleLoadOrder.BeforeCommonModule });

            return builder;
        }
    }
}
