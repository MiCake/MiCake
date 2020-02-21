namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// 
    /// MiCake 模块启动器，用于初始化模块和关闭模块
    /// </summary>
    public interface IMiCakeModuleBoot
    {
        void Initialization(ModuleBearingContext context);

        void ShutDown(ModuleBearingContext context);
    }
}
