namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module lifetime.
    /// When the module is initialized, it is called in turn according to the calling order.
    /// </summary>
    public interface IModuleLifetime
    {
        void PreInitialization(ModuleLoadContext context);

        void Initialization(ModuleLoadContext context);

        void PostInitialization(ModuleLoadContext context);

        void PreShutDown(ModuleLoadContext context);

        void Shutdown(ModuleLoadContext context);
    }
}
