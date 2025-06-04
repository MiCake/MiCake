using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module lifetime.
    /// When the module is initialized, it is called in turn according to the calling order.
    /// </summary>
    public interface IModuleLifetime
    {
        Task PreInitialization(ModuleLoadContext context);

        Task Initialization(ModuleLoadContext context);

        Task PostInitialization(ModuleLoadContext context);

        Task PreShutDown(ModuleLoadContext context);

        Task Shutdown(ModuleLoadContext context);
    }
}
