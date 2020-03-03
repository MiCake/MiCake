namespace MiCake.Core.Modularity
{
    /// <summary>
    /// 
    /// MiCake Module lift cycle.
    /// When the module is started, execute in order
    /// 
    /// 模块生命周期
    /// 在模块启动的时候，按照顺序执行
    /// 
    /// </summary>
    internal interface IModuleLifeTime
    {
        void PreModuleInitialization(ModuleBearingContext context);

        void PostModuleInitialization(ModuleBearingContext context);

        void PreModuleShutDown(ModuleBearingContext context);
    }
}
