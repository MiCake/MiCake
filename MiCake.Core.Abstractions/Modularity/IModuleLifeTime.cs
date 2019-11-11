using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// MiCake Lift Time Interface.When Module initialization,Call this function in sequence.
    /// MiCake生命周期接口。当模块初始化时，按照顺序依次调用
    /// </summary>
    internal interface IModuleLifeTime
    {
        /// <summary>
        /// Called before module startup
        /// 在模块启动前调用
        /// </summary>
        /// <param name="context"><see cref="ModuleContext"/></param>
        void PreStart(ModuleContext context);

        /// <summary>
        /// Calls after module startup and optionAction execution <see cref="IMiCakeApplicationOption"/> 
        /// 在模块启动以及系统配置委托执行完成后调用
        /// </summary>
        /// <param name="context"><see cref="ModuleContext"/></param>
        void OnStart(ModuleContext context);

        /// <summary>
        /// Called before module shuntdown
        /// 在模块结束前调用
        /// </summary>
        /// <param name="context"><see cref="ModuleContext"/></param>
        void PreShuntdown(ModuleContext context);

        /// <summary>
        /// Called when module shuntdown
        /// 在模块结束调用
        /// </summary>
        /// <param name="context"><see cref="ModuleContext"/></param>
        void OnShuntdown(ModuleContext context);

    }
}
