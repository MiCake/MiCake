using MiCake.Cord.Storage;
using MiCake.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Cord.Modules
{
    public static class DDDModuleHelper
    {
        /// <summary>
        /// Be sure can get <see cref="StoreConfig"/> from <see cref="MiCakeDDDModule"/>.
        /// </summary>
        /// <param name="serivces"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static StoreConfig MustGetStoreConfig(IServiceProvider serivces)
        {
            var micakeApp = serivces.GetService<IMiCakeApplication>() ?? throw new InvalidOperationException($"Can not get {nameof(IMiCakeApplication)}.");
            return MustGetStoreConfig(micakeApp);
        }

        /// <summary>
        /// Be sure can get <see cref="StoreConfig"/> from <see cref="MiCakeDDDModule"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static StoreConfig MustGetStoreConfig(IMiCakeApplication application)
        {
            var micakeModules = application.ModuleManager?.ModuleContext;
            if (micakeModules == null)
            {
                throw new InvalidOperationException($"Can not get MiCake module, please check your config is right.");
            }

            var dddModule = micakeModules.MiCakeModules?.FirstOrDefault(s => s.Instance is MiCakeDDDModule);
            if (dddModule == null)
            {
                throw new InvalidOperationException($"Can not get {nameof(MiCakeDDDModule)}, please check your config is right.");
            }

            return (dddModule.Instance as MiCakeDDDModule)!.DomainModelStoreConfig!;
        }
    }
}
