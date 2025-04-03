using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Configure the lifecycle of the module service
    /// When the module is initialized, it is called in turn according to the calling order.
    /// </summary>
    public interface IModuleConfigServicesLifetime
    {
        Task PreConfigServices(ModuleConfigServiceContext context);

        Task ConfigServices(ModuleConfigServiceContext context);

        Task PostConfigServices(ModuleConfigServiceContext context);
    }
}
