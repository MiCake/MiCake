using MiCake.Core.Builder;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class MiCakeBuilderEFCoreExtesion
    {
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
        {
            UseEFCore<TDbContext>(builder, null);
            return builder;
        }

        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions> optionsBulder)
        {
            MiCakeEFCoreOptions options = new MiCakeEFCoreOptions(typeof(TDbContext));
            optionsBulder?.Invoke(options);

            //register the repository
            var eFCoreRepositoryRegister = new EFCoreRepositoryRegister(options);
            eFCoreRepositoryRegister.Register(builder.ModuleManager.MiCakeModules, builder.Services);

            return builder;
        }
    }
}
