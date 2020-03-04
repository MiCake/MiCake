namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake Module lift cycle.
    /// When the module is started, execute in order
    /// </summary>
    public interface IModuleLifeTime
    {
        void PreInitialization(ModuleBearingContext context);

        void Initialization(ModuleBearingContext context);

        void PostInitialization(ModuleBearingContext context);

        void PreShutDown(ModuleBearingContext context);

        void Shutdown(ModuleBearingContext context);
    }
}
