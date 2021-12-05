namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Configure the lifecycle of the module service
    /// When the module is initialized, it is called in turn according to the calling order.
    /// </summary>
    public interface IModuleConfigServicesLifetime
    {
        void PreConfigServices(ModuleConfigServiceContext context);

        void ConfigServices(ModuleConfigServiceContext context);

        void PostConfigServices(ModuleConfigServiceContext context);
    }
}
